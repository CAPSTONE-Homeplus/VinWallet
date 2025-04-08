using AutoMapper;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using RoomProto;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;
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
            // Kiểm tra Username đã tồn tại chưa
            var existUser = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.Username.Equals(createUserRequest.Username));

            if (existUser != null)
                throw new BadHttpRequestException(MessageConstant.UserMessage.UsernameAlreadyExists);

            // Kiểm tra Email đã tồn tại chưa
            var existEmail = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.Email.Equals(createUserRequest.Email));

            if (existEmail != null)
                throw new BadHttpRequestException(MessageConstant.UserMessage.EmailAlreadyExists);

            // Kiểm tra PhoneNumber đã tồn tại chưa
            var existPhoneNumber = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.PhoneNumber.Equals(createUserRequest.PhoneNumber));

            if (existPhoneNumber != null)
                throw new BadHttpRequestException(MessageConstant.UserMessage.PhoneNumberAlreadyExists);

            // Kiểm tra Building và House
            var urlBuilding = HomeCleanApiEndPointConstant.Building.BuildingByCodeEndpoint.Replace("{code}", createUserRequest.BuildingCode);
            var urlHouse = HomeCleanApiEndPointConstant.House.HouseByCodeEndpoint.Replace("{code}", createUserRequest.HouseCode);

            var buildingResponse = await CallApiUtils.CallApiEndpoint(urlBuilding, null);
            var houseResponse = await CallApiUtils.CallApiEndpoint(urlHouse, null);

            var building = await CallApiUtils.GenerateObjectFromResponse<BuildingResponse>(buildingResponse);
            if (building == null)
                throw new BadHttpRequestException(MessageConstant.BuildingMessage.BuildingNotFound);

            var house = await CallApiUtils.GenerateObjectFromResponse<HouseResponse>(houseResponse);
            if (house == null)
                throw new BadHttpRequestException(MessageConstant.HouseMessage.HouseNotFound);

            if (!house.BuildingId.Equals(building.Id))
                throw new BadHttpRequestException(MessageConstant.HouseMessage.HouseNotInBuilding);

            // Tạo User mới
            var newUser = _mapper.Map<User>(createUserRequest);
            newUser.Id = Guid.NewGuid();
            newUser.Password = PasswordUtil.HashPassword(createUserRequest.Password);
            newUser.Status = UserEnum.Status.Active.ToString();
            newUser.HouseId = house.Id;

            // Gán Role mặc định
            Role role = await _unitOfWork.GetRepository<Role>()
                .SingleOrDefaultAsync(predicate: x => x.Name.Equals(UserEnum.Role.Member.ToString()));

            newUser.RoleId = role.Id;
            newUser.CreatedAt = DateTime.UtcNow.AddHours(7);
            newUser.UpdatedAt = DateTime.UtcNow.AddHours(7);

            // Lưu User vào DB
            await _unitOfWork.GetRepository<User>().InsertAsync(newUser);
            if (await _unitOfWork.CommitAsync() <= 0)
                throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);

            // Tạo ví cá nhân cho User
            await _walletService.CreatePersionalWallet(newUser.Id);

            // Trả về Response
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

        public async Task<UserResponse> GetUserByPhoneNumber(string phoneNumber)
        {
            if (phoneNumber == null) throw new BadHttpRequestException(MessageConstant.UserMessage.EmptyPhoneNumber);
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.PhoneNumber.Equals(phoneNumber),
                               include: x => x.Include(x => x.Role));
            if (user == null) throw new BadHttpRequestException(MessageConstant.UserMessage.UserNotFound);
            var response = _mapper.Map<UserResponse>(user);
            return response;
        }

        public async Task<IPaginate<UserResponse>> GetAllUserByShareWalletId(Guid shareWalletId, int page, int limit)
        {
            if (shareWalletId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.WalletMessage.EmptyWalletId);
            var shareWallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(shareWalletId));
            if (shareWallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);
            if (!shareWallet.Type.Equals(WalletEnum.WalletType.Shared.ToString())) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotShare);

            var users = await _unitOfWork.GetRepository<User>().GetPagingListAsync(selector: x => new UserResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Status = x.Status,
                HouseId = x.HouseId,
                ExtraField = x.ExtraField,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Username = x.Username,
                Role = x.Role != null ? x.Role.Name : null,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber
            },
    predicate: x => x.UserWallets.Any(uw => uw.WalletId == shareWalletId),
    include: x => x.Include(x => x.UserWallets),
    page: page,
    size: limit
);
            return users;


        }

        public async Task<bool> CheckUserInfo(string? phoneNumber, string? email, string? username)
        {
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.PhoneNumber.Equals(phoneNumber) || x.Email.Equals(email) || x.Username.Equals(username));

            if (user != null)
            {
                if (user.PhoneNumber.Equals(phoneNumber))
                    throw new BadHttpRequestException(MessageConstant.UserMessage.PhoneNumberAlreadyExists);
                if (user.Email.Equals(email))
                    throw new BadHttpRequestException(MessageConstant.UserMessage.EmailAlreadyExists);
                if (user.Username.Equals(username))
                    throw new BadHttpRequestException(MessageConstant.UserMessage.UsernameAlreadyExists);
            }
            return true;
        }

        public async Task<IPaginate<UserResponse>> GetAllUsers(string? search, string? orderBy, int page, int size)
        {
            Func<IQueryable<User>, IOrderedQueryable<User>> orderByFunc;
            if (!string.IsNullOrEmpty(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "fullname_asc":
                        orderByFunc = x => x.OrderBy(y => y.FullName);
                        break;
                    case "fullname_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.FullName);
                        break;
                    case "username_asc":
                        orderByFunc = x => x.OrderBy(y => y.Username);
                        break;
                    case "username_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.Username);
                        break;
                    case "email_asc":
                        orderByFunc = x => x.OrderBy(y => y.Email);
                        break;
                    case "email_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.Email);
                        break;
                    case "phone_asc":
                        orderByFunc = x => x.OrderBy(y => y.PhoneNumber);
                        break;
                    case "phone_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.PhoneNumber);
                        break;
                    case "status_asc":
                        orderByFunc = x => x.OrderBy(y => y.Status);
                        break;
                    case "status_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.Status);
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
                    default:
                        orderByFunc = x => x.OrderByDescending(y => y.UpdatedAt);
                        break;
                }
            }
            else
            {
                orderByFunc = x => x.OrderByDescending(y => y.UpdatedAt);
            }

            // Query all users with search filter
            var users = await _unitOfWork.GetRepository<User>().GetPagingListAsync(
                selector: x => new UserResponse
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Status = x.Status,
                    HouseId = x.HouseId,
                    ExtraField = x.ExtraField,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    Username = x.Username,
                    Role = x.Role != null ? x.Role.Name : null,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber
                },
                predicate: x => string.IsNullOrEmpty(search) ||
                               x.FullName.Contains(search) ||
                               x.Username.Contains(search) ||
                               x.Email.Contains(search) ||
                               x.PhoneNumber.Contains(search) ||
                               x.Status.Contains(search),
                orderBy: orderByFunc,
                include: x => x.Include(x => x.Role),
                page: page,
                size: size
            );

            return users;
        }
    }
}
