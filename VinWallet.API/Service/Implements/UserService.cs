using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomProto;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Models;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Request.WalletRequest;
using VinWallet.Repository.Payload.Response.UserResponse;
using VinWallet.Repository.Utils;

namespace VinWallet.API.Service.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        private readonly RoomGrpcService.RoomGrpcServiceClient _roomGrpcClient;
        private readonly IWalletService _walletService;
        public UserService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<UserService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, RoomGrpcService.RoomGrpcServiceClient roomGrpcClient, IWalletService walletService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _roomGrpcClient = roomGrpcClient;
            _walletService = walletService;
        }

        public async Task<UserResponse> CreateUser(CreateUserRequest createUserRequest)
        {
            var existUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.Username.Equals(createUserRequest.Username));
            if (existUser != null) throw new BadHttpRequestException(MessageConstant.UserMessage.UsernameAlreadyExists);

            var room = await _roomGrpcClient.GetRoomGrpcAsync(new RoomGrpcRequest { RoomCode = createUserRequest.RoomCode });


            if (room.Id.Equals("")) throw new BadHttpRequestException(MessageConstant.RoomMessage.RoomNotFound);

            var newUser = _mapper.Map<User>(createUserRequest);
            newUser.Id = Guid.NewGuid();
            newUser.Password = PasswordUtil.HashPassword(createUserRequest.Password);
            newUser.Status = UserEnum.Status.Active.ToString();
            newUser.RoomId = Guid.Parse(room.Id);

            var hasRoomLeader = await _unitOfWork.GetRepository<User>().AnyAsync(predicate: x => x.RoomId.ToString().Equals(room.Id));

            Role role;
            if (hasRoomLeader)
            {
                role = await _unitOfWork.GetRepository<Role>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(UserEnum.Role.Member.ToString()));
                newUser.RoleId = role.Id;
            }
            else
            {
                role = await _unitOfWork.GetRepository<Role>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(UserEnum.Role.Leader.ToString()));
                newUser.RoleId = role.Id;
            }

            newUser.CreatedAt = DateTime.UtcNow.AddHours(7);
            newUser.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _unitOfWork.GetRepository<User>().InsertAsync(newUser);
            if (await _unitOfWork.CommitAsync() <= 0) throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);



            var walletRequest = new CreateWalletRequest
            {
                Name = WalletEnum.WalletType.Personal.ToString() + newUser.Username,
                Type = WalletEnum.WalletType.Personal.ToString(),
                OwnerId = newUser.Id
            };
            var wallet = await _walletService.CreateWallet(walletRequest);
            await _walletService.ConnectWalletToUser(newUser.Id, wallet.Id);

            if (role.Name.Equals(UserEnum.Role.Leader.ToString()))
            {

                var walletRequestLeader = new CreateWalletRequest
                {
                    Name = WalletEnum.WalletType.Shared.ToString() + createUserRequest.RoomCode,
                    Type = WalletEnum.WalletType.Shared.ToString(),
                    OwnerId = newUser.Id
                };
                var walletLeader = await _walletService.CreateWallet(walletRequestLeader);
                await _walletService.ConnectWalletToUser(newUser.Id, wallet.Id);

            }
            else
            {

                var leader = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.RoomId.ToString().Equals(room.Id) && x.Role.Name.Equals(UserEnum.Role.Leader.ToString()),
                   include: x => x.Include(x => x.Role));
                var sharedWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.OwnerId.Equals(leader.Id) &&
                x.Type.Equals(WalletEnum.WalletType.Shared.ToString()));
                await _walletService.ConnectWalletToUser(newUser.Id, sharedWallet.Id);

            }

            var response = _mapper.Map<UserResponse>(newUser);
            response.Role = role.Name;
            return response;
        }

        public async Task<UserResponse> GetUserById(Guid id)
        {
            if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.UserMessage.EmptyUserId);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(id),
                include: x => x.Include(x => x.Role));
            var response = _mapper.Map<UserResponse>(user);
            response.Role = user.Role.Name;
            return response;
        }
    }
}
