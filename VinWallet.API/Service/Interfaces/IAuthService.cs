using VinWallet.Repository.Payload.Request;
using VinWallet.Repository.Payload.Response;

namespace VinWallet.API.Service.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest loginRequest);
        Task<LoginResponse> RefreshToken(RefreshTokenRequest refreshTokenRequest);

        Task<LoginResponse> LoginAdmin(LoginRequest loginRequest);
    }
}
