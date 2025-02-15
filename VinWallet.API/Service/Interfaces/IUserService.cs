using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Response.UserResponse;

namespace VinWallet.API.Service.Interfaces
{
    public interface IUserService
    {
        public Task<UserResponse> CreateUser(CreateUserRequest createUserRequest);

        public Task<UserResponse> GetUserById(Guid id);

    }
}
