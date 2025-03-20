using VinWallet.Domain.Paginate;
using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Response.UserResponse;

namespace VinWallet.API.Service.Interfaces
{
    public interface IUserService
    {
        public Task<UserResponse> CreateUser(CreateUserRequest createUserRequest);

        public Task<UserResponse> GetUserById(Guid id);
        public Task<UserResponse> GetUserByPhoneNumber(string phoneNumber);

        public Task<IPaginate<UserResponse>> GetAllUserByShareWalletId(Guid shareWalletId, int page, int limit);

        public Task<bool> CheckUserInfo(string? phoneNumber, string? email, string? username);
        public Task<IPaginate<UserResponse>> GetAllUsers(string? search, string? orderBy, int page, int size);
    }
}
