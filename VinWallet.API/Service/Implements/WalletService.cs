﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

namespace VinWallet.API.Service.Implements
{
    public class WalletService : BaseService<WalletService>, IWalletService
    {
        private readonly RabbitMQPublisher _rabbitMQPublisher;

        public WalletService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<WalletService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, RabbitMQPublisher rabbitMQPublisher) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public async Task<WalletResponse> CreateWallet(CreateWalletRequest createWalletRequest)
        {
            var newWallet = _mapper.Map<Wallet>(createWalletRequest);
            newWallet.Id = Guid.NewGuid();
            newWallet.CreatedAt = DateTime.UtcNow.AddHours(7);
            newWallet.UpdatedAt = DateTime.UtcNow.AddHours(7);
            newWallet.Balance = 0;
            newWallet.Currency = "VND";
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
            if(!wallet.OwnerId.ToString().Equals(userIdFromJwt)) throw new BadHttpRequestException(MessageConstant.WalletMessage.NotAllowAction);

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
                    await _rabbitMQPublisher.Publish("add_wallet_member","vinwallet" ,new InviteWalletMessage
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
    }
}
