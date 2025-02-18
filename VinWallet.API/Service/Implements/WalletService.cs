using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VinWallet.API.Service.Interfaces;
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
        private readonly IVNPayService _vNPayService;
        public WalletService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<WalletService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IVNPayService vNPayService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _vNPayService = vNPayService;
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
            if(id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyWalletId);
            var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate : x => x.Id.Equals(id));
            if(wallet != null && wallet.OwnerId.ToString() != GetUserIdFromJwt()) throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction, StatusCodes.Status403Forbidden);
            return _mapper.Map<WalletResponse>(wallet);
        }

        public async Task<IPaginate<WalletResponse>> GetWalletsOfUser(Guid id, int page, int size)
        {
            if(id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.UserMessage.EmptyUserId);
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

        public async Task<string> TopUpPoints(Guid userId, string amount, Guid walletId)
        {
            if (userId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.UserMessage.EmptyUserId);
            if (string.IsNullOrEmpty(amount)) throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyAmount);

            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.Id == userId);
            if (user == null) throw new BadHttpRequestException(MessageConstant.UserMessage.UserNotFound);

            var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.Id == walletId);
            if (wallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

            int amountInt = int.Parse(amount);
            if (amountInt < 10000)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.MinAmount);


            int points = amountInt / 1000;

            // 🔹 Tạo giao dịch với trạng thái "Pending"
            //var transaction = new WalletTransaction
            //{
            //    Id = Guid.NewGuid(),
            //    UserId = userId,
            //    WalletId = walletId,
            //    Amount = amountInt,
            //    Points = points,
            //    Status = "Pending",
            //    CreatedAt = DateTime.UtcNow.AddHours(7),
            //    UpdatedAt = DateTime.UtcNow.AddHours(7),
            //};

            //await _unitOfWork.GetRepository<WalletTransaction>().InsertAsync(transaction);
            await _unitOfWork.CommitAsync();

            // 🔹 Tạo URL thanh toán VNPay
            string paymentUrl = _vNPayService.GeneratePaymentUrl(amount, Guid.NewGuid().ToString());

            return paymentUrl; // Trả về URL thanh toán
        }


        public Task<WalletResponse> DepositPoints(Guid userId, string amount, Guid walletId)
        {
            throw new NotImplementedException();
        }
    }
}
