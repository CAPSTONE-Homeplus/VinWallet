using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Request.WalletRequest;
using VinWallet.Repository.Payload.Response.WalletResponse;

namespace VinWallet.API.Service.Implements
{
    public class WalletService : BaseService<WalletService>, IWalletService
    {
        public WalletService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<WalletService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
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

        public async Task CreateAndConnectWalletToUser(Guid userId, Role role, Guid roomId)
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
            await ConnectWalletToUser(newUser.Id, wallet.Id);

            if (role.Name.Equals(UserEnum.Role.Leader.ToString()))
            {

                var walletRequestLeader = new CreateWalletRequest
                {
                    Name = WalletEnum.WalletType.Shared.ToString() + roomId.ToString(),
                    Type = WalletEnum.WalletType.Shared.ToString(),
                    OwnerId = newUser.Id
                };
                var walletLeader = await CreateWallet(walletRequestLeader);
                await ConnectWalletToUser(newUser.Id, wallet.Id);

            }
            else
            {
                var leader = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.RoomId.Equals(roomId) && x.Role.Name.Equals(UserEnum.Role.Leader.ToString()),
                   include: x => x.Include(x => x.Role));
                var sharedWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.OwnerId.Equals(leader.Id) &&
                x.Type.Equals(WalletEnum.WalletType.Shared.ToString()));
                await ConnectWalletToUser(newUser.Id, sharedWallet.Id);
            }
        }
    }
}
