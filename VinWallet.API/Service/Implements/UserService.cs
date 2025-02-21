using AutoMapper;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using RoomProto;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Models;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Request.WalletRequest;
using VinWallet.Repository.Payload.Response.BuildingResponse;
using VinWallet.Repository.Payload.Response.HouseResponse;
using VinWallet.Repository.Payload.Response.RoomResponse;
using VinWallet.Repository.Payload.Response.UserResponse;
using VinWallet.Repository.Utils;

namespace VinWallet.API.Service.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        private readonly RoomGrpcService.RoomGrpcServiceClient _roomGrpcClient;
        private readonly IWalletService _walletService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        public UserService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<UserService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, RoomGrpcService.RoomGrpcServiceClient roomGrpcClient, IWalletService walletService, IBackgroundJobClient backgroundJobClient) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _roomGrpcClient = roomGrpcClient;
            _walletService = walletService;
            _backgroundJobClient = backgroundJobClient;

        }

        public async Task<UserResponse> CreateUser(CreateUserRequest createUserRequest)
        {
            var existUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.Username.Equals(createUserRequest.Username));
            if (existUser != null) throw new BadHttpRequestException(MessageConstant.UserMessage.UsernameAlreadyExists);

            var urlBuilding = HomeCleanApiEndPointConstant.Building.BuildingByCodeEndpoint.Replace("{code}", createUserRequest.BuildingCode);
            var urlHouse = HomeCleanApiEndPointConstant.House.HouseByCodeEndpoint.Replace("{code}", createUserRequest.HouseCode);

            var buildingResponse = await CallApiUtils.CallApiEndpoint(urlBuilding, null);
            var houseResponse = await CallApiUtils.CallApiEndpoint(urlHouse, null);

            var buiding = await CallApiUtils.GenerateObjectFromResponse<BuildingResponse>(buildingResponse);
            if (buiding == null) throw new BadHttpRequestException(MessageConstant.BuildingMessage.BuildingNotFound);

            var house = await CallApiUtils.GenerateObjectFromResponse<HouseResponse>(houseResponse);
            if (house == null) throw new BadHttpRequestException(MessageConstant.HouseMessage.HouseNotFound);

            if(!house.BuildingId.Equals(buiding.Id)) throw new BadHttpRequestException(MessageConstant.HouseMessage.HouseNotInBuilding);


            var newUser = _mapper.Map<User>(createUserRequest);
            newUser.Id = Guid.NewGuid();
            newUser.Password = PasswordUtil.HashPassword(createUserRequest.Password);
            newUser.Status = UserEnum.Status.Active.ToString();
            newUser.HouseId = house.Id;

            var hasRoomLeader = await _unitOfWork.GetRepository<User>().AnyAsync(predicate: x => x.HouseId.Equals(house.Id));

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

            _backgroundJobClient.Enqueue(() => _walletService.CreateAndConnectWalletToUser(newUser.Id, role, house.Id));

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
