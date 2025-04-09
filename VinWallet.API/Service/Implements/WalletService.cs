using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using VinWallet.API.Hubs.Message;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Service.RabbitMQ;
using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Request.WalletRequest;
using VinWallet.Repository.Payload.Response.WalletResponse;
using static VinWallet.Repository.Enums.TransactionCategoryEnum;

namespace VinWallet.API.Service.Implements
{
    public class WalletService : BaseService<WalletService>, IWalletService
    {
        private readonly RabbitMQPublisher _rabbitMQPublisher;
        private readonly ISignalRHubService _signalRHubService;

        public WalletService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<WalletService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, RabbitMQPublisher rabbitMQPublisher, ISignalRHubService signalRHubService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _rabbitMQPublisher = rabbitMQPublisher;
            _signalRHubService = signalRHubService;
        }

        public async Task<WalletResponse> CreateWallet(CreateWalletRequest createWalletRequest)
        {
            var newWallet = _mapper.Map<Wallet>(createWalletRequest);
            newWallet.Id = Guid.NewGuid();
            newWallet.CreatedAt = DateTime.UtcNow.AddHours(7);
            newWallet.UpdatedAt = DateTime.UtcNow.AddHours(7);
            newWallet.Balance = 0;
            newWallet.Currency = "POINT";
            newWallet.Status = WalletEnum.WalletStatus.Active.ToString();
            await _unitOfWork.GetRepository<Wallet>().InsertAsync(newWallet);
            if (await _unitOfWork.CommitAsync() <= 0) throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
            return _mapper.Map<WalletResponse>(newWallet);
        }

        public async Task<WalletResponse> GetWalletById(Guid id)
        {
            if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyWalletId);
            var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(id));
            if (wallet != null && wallet.OwnerId.ToString() != GetUserIdFromJwt()) throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction, StatusCodes.Status403Forbidden);
            return _mapper.Map<WalletResponse>(wallet);
        }

        public async Task<IPaginate<WalletResponse>> GetWalletsOfUser(Guid id, int page, int size)
        {
            if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.UserMessage.EmptyUserId);
            var userid = GetUserIdFromJwt();
            if (id.ToString() != GetUserIdFromJwt()) throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction, StatusCodes.Status403Forbidden);
            var wallets = await _unitOfWork.GetRepository<UserWallet>()
                    .GetPagingListAsync(
                        predicate: x => x.UserId == id,
                        selector: x => new WalletResponse
                        {
                            Id = x.Wallet.Id,
                            Name = x.Wallet.Name,
                            Balance = x.Wallet.Balance,
                            Currency = x.Wallet.Currency,
                            Type = x.Wallet.Type,
                            ExtraField = x.Wallet.ExtraField,
                            UpdatedAt = x.Wallet.UpdatedAt,
                            CreatedAt = x.Wallet.CreatedAt,
                            Status = x.Wallet.Status,
                            OwnerId = x.Wallet.OwnerId
                        },
                        include: x => x.Include(x => x.Wallet),
                        page: page,
                        size: size
                    );
            return wallets;
        }

        public async Task<bool> ConnectWalletToUser(Guid? userId, Guid walletId)
        {
            var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(walletId));
            if (wallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);
            if (wallet.Type.Equals(WalletEnum.WalletType.Personal.ToString())) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotShare);
            var userIdFromJwt = GetUserIdFromJwt();
            if (!wallet.OwnerId.ToString().Equals(userIdFromJwt)) throw new BadHttpRequestException(MessageConstant.WalletMessage.NotAllowAction);

            var userWallets = await _unitOfWork.GetRepository<UserWallet>().GetListAsync(predicate: x => x.UserId.Equals(userId));

            if (userWallets.Count() < 2)
            {
                var userWallet = new UserWallet
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    UpdatedAt = DateTime.UtcNow.AddHours(7),
                    Status = WalletEnum.UserWalletStatus.Joined.ToString(),
                    WalletId = walletId
                };

                await _unitOfWork.GetRepository<UserWallet>().InsertAsync(userWallet);


            }
            else
            {
                throw new BadHttpRequestException(MessageConstant.WalletMessage.UserHasShareWallet);
            }
            if (await _unitOfWork.CommitAsync() <= 0) throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _rabbitMQPublisher.Publish("add_wallet_member", "vinwallet", new InviteWalletMessage
                    {
                        WalletId = walletId,
                        OwnerId = wallet.OwnerId,
                        MemberId = userId
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ RabbitMQ publish failed: {ex.Message}");
                }
            });

            return true;
        }

        public async Task<bool> ConnectWalletToUserForCreateUser(Guid? userId, Guid walletId)
        {
            var userWallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                UpdatedAt = DateTime.UtcNow.AddHours(7),
                Status = WalletEnum.UserWalletStatus.Joined.ToString(),
                WalletId = walletId
            };

            await _unitOfWork.GetRepository<UserWallet>().InsertAsync(userWallet);
            if (await _unitOfWork.CommitAsync() <= 0) throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
            return true;
        }

        public async Task CreatePersionalWallet(Guid userId)
        {
            var newUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(userId),
                include: x => x.Include(x => x.Role));

            var walletRequest = new CreateWalletRequest
            {
                Name = WalletEnum.WalletType.Personal.ToString() + newUser.Username,
                Type = WalletEnum.WalletType.Personal.ToString(),
                OwnerId = newUser.Id
            };

            var wallet = await CreateWallet(walletRequest);
            await ConnectWalletToUserForCreateUser(newUser.Id, wallet.Id);

            //if (role.Name.Equals(UserEnum.Role.Leader.ToString()))
            //{

            //    var walletRequestLeader = new CreateWalletRequest
            //    {
            //        Name = WalletEnum.WalletType.Shared.ToString() + houseId.ToString(),
            //        Type = WalletEnum.WalletType.Shared.ToString(),
            //        OwnerId = newUser.Id
            //    };
            //    var walletLeader = await CreateWallet(walletRequestLeader);
            //    await ConnectWalletToUser(newUser.Id, walletLeader.Id);

            //}
            //else
            //{
            //    var leader = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.HouseId.Equals(houseId) && x.Role.Name.Equals(UserEnum.Role.Leader.ToString()),
            //       include: x => x.Include(x => x.Role));
            //    var sharedWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.OwnerId.Equals(leader.Id) &&
            //    x.Type.Equals(WalletEnum.WalletType.Shared.ToString()));
            //    await ConnectWalletToUser(newUser.Id, sharedWallet.Id);
            //}
        }


        public async Task<bool> UpdateWalletBalance(Guid walletId, string amount, TransactionCategoryEnum.TransactionCategory transactionCategory)
        {
            if (walletId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyWalletId);
            var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(walletId));
            if (wallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);
            if (string.IsNullOrWhiteSpace(amount))
                throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyAmount);

            if (!decimal.TryParse(amount, out var parsedAmount))
                throw new BadHttpRequestException(MessageConstant.WalletMessage.InvalidAmount);
            if (transactionCategory == TransactionCategoryEnum.TransactionCategory.Deposit)
            {
                wallet.Balance += decimal.Parse(amount);
            }
            else
            {
                wallet.Balance -= decimal.Parse(amount);
                if (wallet.Balance < 0) throw new BadHttpRequestException(MessageConstant.WalletMessage.NotEnoughBalance);
            }
            wallet.UpdatedAt = DateTime.UtcNow.AddHours(7);
            _unitOfWork.GetRepository<Wallet>().UpdateAsync(wallet);
            if (await _unitOfWork.CommitAsync() <= 0) return false;
            return true;
        }

        public async Task<WalletResponse> CreateShareWallet(Guid userId)
        {
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(userId));

            var hasShareWallet = await _unitOfWork.GetRepository<UserWallet>().AnyAsync(predicate: x => x.UserId.Equals(userId) && x.Wallet.Type.Equals(WalletEnum.WalletType.Shared.ToString()));
            if (hasShareWallet) throw new BadHttpRequestException(MessageConstant.WalletMessage.UserHasSharedWallet);

            var walletRequestLeader = new CreateWalletRequest
            {
                Name = WalletEnum.WalletType.Shared.ToString() + user.HouseId.ToString(),
                Type = WalletEnum.WalletType.Shared.ToString(),
                OwnerId = userId
            };
            var walletLeader = await CreateWallet(walletRequestLeader);
            await ConnectWalletToUser(userId, walletLeader.Id);
            return _mapper.Map<WalletResponse>(walletLeader);

            //else
            //{
            //    var leader = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.HouseId.Equals(houseId) && x.Role.Name.Equals(UserEnum.Role.Leader.ToString()),
            //       include: x => x.Include(x => x.Role));
            //    var sharedWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.OwnerId.Equals(leader.Id) &&
            //    x.Type.Equals(WalletEnum.WalletType.Shared.ToString()));
            //    await ConnectWalletToUser(newUser.Id, sharedWallet.Id);
            //}
        }

        public async Task<bool> DeleteUserWallet(Guid userId, Guid walletId)
        {
            var userWallet = await _unitOfWork.GetRepository<UserWallet>().SingleOrDefaultAsync(predicate: x => x.UserId.Equals(userId) && x.WalletId.Equals(walletId));
            if (userWallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.UserNotInWallet);
            _unitOfWork.GetRepository<UserWallet>().DeleteAsync(userWallet);
            if (await _unitOfWork.CommitAsync() <= 0) return false;
            return true;
        }

        public async Task<WalletResponse> UpdateOwnerId(Guid walletId, Guid userId)
        {
            var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(walletId));
            if (wallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);
            wallet.OwnerId = userId;
            wallet.UpdatedAt = DateTime.UtcNow.AddHours(7);
            _unitOfWork.GetRepository<Wallet>().UpdateAsync(wallet);
            if (await _unitOfWork.CommitAsync() <= 0) throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
            return _mapper.Map<WalletResponse>(wallet);
        }


        public async Task<WalletContributionResponse> GetWalletContributionStatistics(Guid walletId, int days)
        {
            if (walletId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyWalletId);

            var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(walletId) && x.Type.Equals(WalletEnum.WalletType.Shared.ToString()),
                include: x => x.Include(w => w.UserWallets).ThenInclude(uw => uw.User)
            );

            if (wallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

            // Calculate date range for the specified number of days
            DateTime endDate = DateTime.UtcNow.AddHours(7); // Using your timezone adjustment pattern
            DateTime startDate = endDate.AddDays(-days);


            // Get all deposits made to the wallet in the specified time period
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.WalletId.Equals(walletId) &&
                                 x.Category.Name.Equals(TransactionCategoryEnum.TransactionCategory.Deposit.ToString()) &&
                                 x.CreatedAt >= startDate && x.CreatedAt <= endDate
            );

            // Calculate total contribution
            decimal totalContribution = transactions.Sum(t => Convert.ToInt64(t.Amount));

            // Group transactions by user and calculate contributions
            var userContributions = transactions
                .GroupBy(t => t.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Contribution = g.Sum(t => Convert.ToInt64(t.Amount))
                })
                .ToList();

            // Map to response model
            var members = new List<MemberContributionDto>();

            foreach (var userContribution in userContributions)
            {
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: x => x.Id.Equals(userContribution.UserId)
                );

                if (user != null)
                {
                    decimal percentage = totalContribution > 0
                        ? Math.Round((userContribution.Contribution / totalContribution) * 100, 1)
                        : 0;
                    members.Add(new MemberContributionDto
                    {
                        Name = user.FullName ?? $"{user.FullName}",
                        Contribution = userContribution.Contribution,
                        Percentage = percentage,
                    });
                }
            }

            // Sort by contribution amount (descending)
            members = members.OrderByDescending(m => m.Contribution).ToList();

            // Format time frame text
            string timeFrame;
            if (days == 30)
                timeFrame = "Tháng này"; // This month
            else if (days == 7)
                timeFrame = "Tuần này";  // This week
            else
                timeFrame = $"{days} ngày gần nhất"; // Last X days

            return new WalletContributionResponse
            {
                TotalContribution = totalContribution,
                TimeFrame = timeFrame,
                Members = members
            };
        }

        public async Task<IPaginate<WalletResponse>> GetAllWallets(string? search, string? orderBy, int page, int size)
        {
            Func<IQueryable<Wallet>, IOrderedQueryable<Wallet>> orderByFunc;
            if (!string.IsNullOrEmpty(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "name_asc":
                        orderByFunc = x => x.OrderBy(y => y.Name);
                        break;
                    case "name_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.Name);
                        break;
                    case "balance_asc":
                        orderByFunc = x => x.OrderBy(y => y.Balance);
                        break;
                    case "balance_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.Balance);
                        break;
                    case "created_asc":
                        orderByFunc = x => x.OrderBy(y => y.CreatedAt);
                        break;
                    case "created_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.CreatedAt);
                        break;
                    case "updated_asc":
                        orderByFunc = x => x.OrderBy(y => y.UpdatedAt);
                        break;
                    case "updated_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.UpdatedAt);
                        break;
                    case "type_asc":
                        orderByFunc = x => x.OrderBy(y => y.Type);
                        break;
                    case "type_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.Type);
                        break;
                    default:
                        orderByFunc = x => x.OrderByDescending(y => y.UpdatedAt);
                        break;
                }
            }
            else
            {
                orderByFunc = x => x.OrderByDescending(y => y.UpdatedAt);
            }

            // Query all wallets with search filter
            var wallets = await _unitOfWork.GetRepository<Wallet>().GetPagingListAsync(
                selector: x => _mapper.Map<WalletResponse>(x),
                predicate: x => string.IsNullOrEmpty(search) ||
                               x.Name.Contains(search) ||
                               x.Currency.Contains(search) ||
                               x.Type.Contains(search) ||
                               x.Status.Contains(search),
                orderBy: orderByFunc,
                page: page,
                size: size
            );

            return wallets;
        }

        public async Task<bool> WalletDissolution(Guid walletId)
        {
            try
            {
                // Kiểm tra đầu vào
                if (walletId == Guid.Empty)
                    throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyWalletId);

                // Tìm ví cần giải thể
                var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(walletId));
                if (wallet == null)
                    throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

                // Kiểm tra loại ví
                if (wallet.Type.Equals(WalletEnum.WalletType.Personal.ToString()))
                    throw new BadHttpRequestException(MessageConstant.WalletMessage.CannotDissolvePersonalWallet);

                // Kiểm tra trạng thái ví
                if (wallet.Status.Equals(WalletEnum.WalletStatus.Dissolved.ToString()))
                    throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletAlreadyDissolved);

                if (wallet.Status.Equals(WalletEnum.WalletStatus.Inactive.ToString()))
                    throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletInactive);

                // Kiểm tra quyền người dùng
                var userIdFromJwt = GetUserIdFromJwt();
                if (string.IsNullOrEmpty(userIdFromJwt))
                    throw new UnauthorizedAccessException(MessageConstant.UserMessage.UnauthorizedUser);

                if (wallet.OwnerId.ToString() != userIdFromJwt)
                    throw new BadHttpRequestException(MessageConstant.WalletMessage.NotAllowAction, StatusCodes.Status403Forbidden);

                // Tìm ví cá nhân của chủ sở hữu
                var personalWalletOfOwner = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(
                    predicate: x => x.OwnerId.Equals(wallet.OwnerId) &&
                                 x.Type.Equals(WalletEnum.WalletType.Personal.ToString()) &&
                                 x.Status.Equals(WalletEnum.WalletStatus.Active.ToString())
                );

                if (personalWalletOfOwner == null)
                    throw new BadHttpRequestException(MessageConstant.WalletMessage.PersonalWalletNotFound);

                decimal? walletBalance = wallet.Balance;
                bool hasBalance = walletBalance > 0;

                Category withdrawCategory = null;
                Category depositCategory = null;

                if (hasBalance)
                {
                    // Tìm danh mục giao dịch
                    withdrawCategory = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                        predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Withdraw.ToString()) &&
                                     x.Status.Equals("Active")
                    );

                    depositCategory = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                        predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Deposit.ToString()) &&
                                     x.Status.Equals("Active")
                    );

                    if (withdrawCategory == null || depositCategory == null)
                    {
                        throw new BadHttpRequestException(MessageConstant.TransactionMessage.CategoryNotFound);
                    }
                }

                // Bắt đầu xử lý chính
                if (hasBalance)
                {
                    // Cập nhật số dư ví cá nhân
                    decimal? newBalance = personalWalletOfOwner.Balance + wallet.Balance;

                    // Kiểm tra tràn số
                    if (newBalance < personalWalletOfOwner.Balance) // Kiểm tra tràn số
                        throw new InvalidOperationException(MessageConstant.WalletMessage.BalanceOverflow);

                    personalWalletOfOwner.Balance = newBalance;
                    personalWalletOfOwner.UpdatedAt = DateTime.UtcNow.AddHours(7);
                    _unitOfWork.GetRepository<Wallet>().UpdateAsync(personalWalletOfOwner);

                    // Tạo các mã giao dịch
                    var randomWD = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                    var randomDP = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                    var currentTime = DateTime.UtcNow.AddHours(7);

                    // Tạo giao dịch rút tiền từ ví chung
                    var withdrawTransaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = walletId, // Sửa lại từ wallet.OwnerId thành walletId
                        UserId = wallet.OwnerId, // Sửa lại từ walletId thành wallet.OwnerId  
                        Amount = walletBalance.ToString(),
                        Type = TransactionEnum.TransactionType.Withdraw.ToString(),
                        Note = $"Rút tiền do giải thể ví",
                        TransactionDate = currentTime,
                        Status = TransactionEnum.TransactionStatus.Success.ToString(),
                        CreatedAt = currentTime,
                        UpdatedAt = currentTime,
                        Code = $"WD-{currentTime:yyyyMMddHHmmssfff}-{randomWD}",
                        CategoryId = withdrawCategory.Id,
                    };
                    await _unitOfWork.GetRepository<Transaction>().InsertAsync(withdrawTransaction);

                    // Tạo giao dịch nạp tiền vào ví cá nhân
                    var depositTransaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = personalWalletOfOwner.Id,
                        UserId = wallet.OwnerId,
                        Amount = walletBalance.ToString(),
                        Type = TransactionEnum.TransactionType.Deposit.ToString(),
                        Note = $"Nhận tiền từ ví đã giải thể: {wallet.Name}",
                        TransactionDate = currentTime,
                        Status = TransactionEnum.TransactionStatus.Success.ToString(),
                        CreatedAt = currentTime,
                        UpdatedAt = currentTime,
                        Code = $"DP-{currentTime:yyyyMMddHHmmssfff}-{randomDP}",
                        CategoryId = depositCategory.Id
                    };
                    await _unitOfWork.GetRepository<Transaction>().InsertAsync(depositTransaction);
                }

                // Cập nhật trạng thái ví
                wallet.Balance = 0;
                wallet.Status = WalletEnum.WalletStatus.Dissolved.ToString();
                wallet.UpdatedAt = DateTime.UtcNow.AddHours(7);
                _unitOfWork.GetRepository<Wallet>().UpdateAsync(wallet);

                // Xử lý các kết nối UserWallet
                var userWallets = await _unitOfWork.GetRepository<UserWallet>().GetListAsync(
                    predicate: x => x.WalletId.Equals(walletId)
                );

                if (userWallets == null || !userWallets.Any())
                {
                    Console.WriteLine($"Warning: No UserWallet connections found for wallet {walletId}");
                }

                var userIds = userWallets.Select(x => x.UserId.ToString()).Distinct().ToList();

                foreach (var userWallet in userWallets)
                {
                    _unitOfWork.GetRepository<UserWallet>().DeleteAsync(userWallet);
                }

                // Commit mọi thay đổi
                int commitResult = await _unitOfWork.CommitAsync();
                if (commitResult <= 0)
                    throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);

                // Gửi thông báo qua SignalR
                try
                {
                    await _signalRHubService.SendNotificationToUsers(userIds, "Trưởng nhóm đã hủy ví chung");
                    await _signalRHubService.SendNotificationToUser(wallet.OwnerId.ToString(), "Bạn đã hủy ví chung");
                }
                catch (Exception signalREx)
                {
                    // Log lỗi nhưng không ảnh hưởng đến kết quả
                    Console.WriteLine($" SignalR notification failed: {signalREx.Message}");
                }

                // Gửi thông báo qua RabbitMQ
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _rabbitMQPublisher.Publish("wallet_dissolution", "vinwallet", new
                        {
                            WalletId = walletId,
                            WalletName = wallet.Name,
                            OwnerId = wallet.OwnerId,
                            TransferredAmount = walletBalance,
                            MemberIds = userIds,
                            DissolvedAt = wallet.UpdatedAt
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" RabbitMQ publish failed: {ex.Message}");
                    }
                });

                return true;
            }
            catch (BadHttpRequestException ex)
            {
                // Chuyển tiếp lỗi BadHttpRequestException
                Console.WriteLine($"Bad request error dissolving wallet {walletId}: {ex.Message}");
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                // Xử lý lỗi xác thực
                Console.WriteLine($"Authorization error dissolving wallet {walletId}: {ex.Message}");
                throw;
            }
            catch (DbUpdateException ex)
            {
                // Xử lý lỗi database
                Console.WriteLine($"Database error dissolving wallet {walletId}: {ex.Message}");
                throw new Exception("Lỗi cơ sở dữ liệu khi giải thể ví, vui lòng thử lại sau.", ex);
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                Console.WriteLine($"Error dissolving wallet {walletId}: {ex.Message}");
                throw new Exception("Lỗi khi giải thể ví, vui lòng thử lại.", ex);
            }
        }

        public async Task<bool> TransferFromSharedToPersonal(Guid sharedWalletId, Guid personalWalletId, decimal amount)
        {
            if (sharedWalletId == Guid.Empty || personalWalletId == Guid.Empty)
            {
                throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyWalletId);
            }
            if (amount <= 0)
            {
                throw new BadHttpRequestException(MessageConstant.WalletMessage.InvalidAmount);
            }

            var sharedWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(sharedWalletId) && x.Type.Equals(WalletEnum.WalletType.Shared.ToString()),
                include: x => x.Include(w => w.UserWallets)
            );
            if (sharedWallet == null)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);
            if (!sharedWallet.Status.Equals(WalletEnum.WalletStatus.Active.ToString()))
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletInactive);

            var personalWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(personalWalletId) && x.Type.Equals(WalletEnum.WalletType.Personal.ToString())
            );
            if (personalWallet == null)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);
            if (!personalWallet.Status.Equals(WalletEnum.WalletStatus.Active.ToString()))
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletInactive);
            var userIdFromJwt = GetUserIdFromJwt();
            var userId = Guid.Parse(userIdFromJwt);

            bool isSharedWalletMember = sharedWallet.UserWallets.Any(uw => uw.UserId.Equals(userId));
            if (!isSharedWalletMember)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.UserNotInWallet, StatusCodes.Status403Forbidden);

            if (!personalWallet.OwnerId.ToString().Equals(userIdFromJwt))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction, StatusCodes.Status403Forbidden);

            if (sharedWallet.Balance < amount)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.NotEnoughBalance);

            var withdrawCategory = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Withdraw.ToString()) &&
                             x.Status.Equals("Active")
            );
            var depositCategory = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Deposit.ToString()) &&
                             x.Status.Equals("Active")
            );

            if (withdrawCategory == null || depositCategory == null)
                throw new BadHttpRequestException(MessageConstant.TransactionMessage.CategoryNotFound);

            try
            {
                // Generate transaction codes
                var randomWD = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                var randomDP = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                var currentTime = DateTime.UtcNow.AddHours(7);

                // Update shared wallet balance
                sharedWallet.Balance -= amount;
                sharedWallet.UpdatedAt = currentTime;
                _unitOfWork.GetRepository<Wallet>().UpdateAsync(sharedWallet);

                // Create withdraw transaction for shared wallet
                var withdrawTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = sharedWalletId,
                    UserId = userId,
                    Amount = amount.ToString(),
                    Type = TransactionEnum.TransactionType.Withdraw.ToString(),
                    Note = $"Rút tiền từ ví chung {sharedWallet.Name} sang ví cá nhân",
                    TransactionDate = currentTime,
                    Status = TransactionEnum.TransactionStatus.Success.ToString(),
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime,
                    Code = $"WD-{currentTime:yyyyMMddHHmmssfff}-{randomWD}",
                    CategoryId = withdrawCategory.Id
                };
                await _unitOfWork.GetRepository<Transaction>().InsertAsync(withdrawTransaction);

                // Update personal wallet balance
                personalWallet.Balance += amount;
                personalWallet.UpdatedAt = currentTime;
                _unitOfWork.GetRepository<Wallet>().UpdateAsync(personalWallet);

                // Create deposit transaction for personal wallet
                var depositTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = personalWalletId,
                    UserId = userId,
                    Amount = amount.ToString(),
                    Type = TransactionEnum.TransactionType.Deposit.ToString(),
                    Note = $"Nhận tiền từ ví chung {sharedWallet.Name}",
                    TransactionDate = currentTime,
                    Status = TransactionEnum.TransactionStatus.Success.ToString(),
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime,
                    Code = $"DP-{currentTime:yyyyMMddHHmmssfff}-{randomDP}",
                    CategoryId = depositCategory.Id
                };
                await _unitOfWork.GetRepository<Transaction>().InsertAsync(depositTransaction);

                // Commit all changes
                if (await _unitOfWork.CommitAsync() <= 0)
                    throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);

                // Notify members of shared wallet about the transfer
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var memberIds = sharedWallet.UserWallets
                            .Where(uw => !uw.UserId.Equals(userId))
                            .Select(uw => uw.UserId.ToString())
                            .ToList();

                        if (memberIds.Any())
                        {
                            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                                predicate: x => x.Id.Equals(userId)
                            );

                            string userName = user?.FullName ?? user?.Username ?? "Một thành viên";
                            string message = $"{userName} đã chuyển {amount} từ ví chung {sharedWallet.Name} sang ví cá nhân";

                            await _signalRHubService.SendNotificationToUsers(memberIds, message);
                        }

                        // Publish message to RabbitMQ
                        await _rabbitMQPublisher.Publish("wallet_transfer", "vinwallet", new
                        {
                            SourceWalletId = sharedWalletId,
                            DestinationWalletId = personalWalletId,
                            UserId = userId,
                            Amount = amount,
                            TransferredAt = currentTime
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Notification error: {ex.Message}");
                        // Don't throw exception as this is a background task
                    }
                });

                return true;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error transferring from wallet {sharedWalletId} to {personalWalletId}: {ex.Message}");
                throw new Exception("Lỗi cơ sở dữ liệu khi chuyển tiền, vui lòng thử lại sau.", ex);
            }
            catch (BadHttpRequestException)
            {
                // Re-throw BadHttpRequestException as it already contains proper error message
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error transferring from wallet {sharedWalletId} to {personalWalletId}: {ex.Message}");
                throw new Exception("Lỗi khi chuyển tiền, vui lòng thử lại.", ex);
            }
        }

        public async Task<bool> TransferFromPersonalToShared(Guid personalWalletId, Guid sharedWalletId, decimal amount)
        {
            if (personalWalletId == Guid.Empty || sharedWalletId == Guid.Empty)
            {
                throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyWalletId);
            }
            if (amount <= 0)
            {
                throw new BadHttpRequestException(MessageConstant.WalletMessage.InvalidAmount);
            }

            var personalWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(personalWalletId) && x.Type.Equals(WalletEnum.WalletType.Personal.ToString())
            );
            if (personalWallet == null)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);
            if (!personalWallet.Status.Equals(WalletEnum.WalletStatus.Active.ToString()))
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletInactive);

            var sharedWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(sharedWalletId) && x.Type.Equals(WalletEnum.WalletType.Shared.ToString()),
                include: x => x.Include(w => w.UserWallets)
            );
            if (sharedWallet == null)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);
            if (!sharedWallet.Status.Equals(WalletEnum.WalletStatus.Active.ToString()))
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletInactive);

            var userIdFromJwt = GetUserIdFromJwt();
            var userId = Guid.Parse(userIdFromJwt);

            // Verify the user owns the personal wallet
            if (!personalWallet.OwnerId.ToString().Equals(userIdFromJwt))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction, StatusCodes.Status403Forbidden);

            // Verify the user is a member of the shared wallet
            bool isSharedWalletMember = sharedWallet.UserWallets.Any(uw => uw.UserId.Equals(userId));
            if (!isSharedWalletMember)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.UserNotInWallet, StatusCodes.Status403Forbidden);

            // Check if personal wallet has enough balance
            if (personalWallet.Balance < amount)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.NotEnoughBalance);

            // Get transaction categories
            var withdrawCategory = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Withdraw.ToString()) &&
                             x.Status.Equals("Active")
            );
            var depositCategory = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
                predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Deposit.ToString()) &&
                             x.Status.Equals("Active")
            );

            if (withdrawCategory == null || depositCategory == null)
                throw new BadHttpRequestException(MessageConstant.TransactionMessage.CategoryNotFound);

            try
            {
                // Generate transaction codes
                var randomWD = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                var randomDP = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                var currentTime = DateTime.UtcNow.AddHours(7);

                // Update personal wallet balance
                personalWallet.Balance -= amount;
                personalWallet.UpdatedAt = currentTime;
                _unitOfWork.GetRepository<Wallet>().UpdateAsync(personalWallet);

                // Create withdraw transaction for personal wallet
                var withdrawTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = personalWalletId,
                    UserId = userId,
                    Amount = amount.ToString(),
                    Type = TransactionEnum.TransactionType.Withdraw.ToString(),
                    Note = $"Chuyển tiền từ ví cá nhân sang ví chung {sharedWallet.Name}",
                    TransactionDate = currentTime,
                    Status = TransactionEnum.TransactionStatus.Success.ToString(),
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime,
                    Code = $"WD-{currentTime:yyyyMMddHHmmssfff}-{randomWD}",
                    CategoryId = withdrawCategory.Id
                };
                await _unitOfWork.GetRepository<Transaction>().InsertAsync(withdrawTransaction);

                // Update shared wallet balance
                sharedWallet.Balance += amount;
                sharedWallet.UpdatedAt = currentTime;
                _unitOfWork.GetRepository<Wallet>().UpdateAsync(sharedWallet);

                // Create deposit transaction for shared wallet
                var depositTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = sharedWalletId,
                    UserId = userId,
                    Amount = amount.ToString(),
                    Type = TransactionEnum.TransactionType.Deposit.ToString(),
                    Note = $"Nhận tiền từ ví cá nhân của {personalWallet.OwnerId}",
                    TransactionDate = currentTime,
                    Status = TransactionEnum.TransactionStatus.Success.ToString(),
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime,
                    Code = $"DP-{currentTime:yyyyMMddHHmmssfff}-{randomDP}",
                    CategoryId = depositCategory.Id
                };
                await _unitOfWork.GetRepository<Transaction>().InsertAsync(depositTransaction);

                // Commit all changes
                if (await _unitOfWork.CommitAsync() <= 0)
                    throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);

                // Notify members of shared wallet about the contribution
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Get user information for better notification
                        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                            predicate: x => x.Id.Equals(userId)
                        );

                        string userName = user?.FullName ?? user?.Username ?? "Một thành viên";

                        // Get all other members of the shared wallet
                        var memberIds = sharedWallet.UserWallets
                            .Where(uw => !uw.UserId.Equals(userId))
                            .Select(uw => uw.UserId.ToString())
                            .ToList();

                        if (memberIds.Any())
                        {
                            string message = $"{userName} đã đóng góp {amount} vào ví chung {sharedWallet.Name}";
                            await _signalRHubService.SendNotificationToUsers(memberIds, message);
                        }

                        // Publish message to RabbitMQ
                        await _rabbitMQPublisher.Publish("wallet_contribution", "vinwallet", new
                        {
                            SourceWalletId = personalWalletId,
                            DestinationWalletId = sharedWalletId,
                            UserId = userId,
                            Amount = amount,
                            ContributedAt = currentTime
                        });

                        // Update contribution statistics if needed
                        // This could potentially trigger an update to the wallet contribution statistics
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Notification error: {ex.Message}");
                        // Don't throw exception as this is a background task
                    }
                });

                return true;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error transferring from wallet {personalWalletId} to {sharedWalletId}: {ex.Message}");
                throw new Exception("Lỗi cơ sở dữ liệu khi chuyển tiền, vui lòng thử lại sau.", ex);
            }
            catch (BadHttpRequestException)
            {
                // Re-throw BadHttpRequestException as it already contains proper error message
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error transferring from wallet {personalWalletId} to {sharedWalletId}: {ex.Message}");
                throw new Exception("Lỗi khi chuyển tiền, vui lòng thử lại.", ex);
            }
        }
    }
}
