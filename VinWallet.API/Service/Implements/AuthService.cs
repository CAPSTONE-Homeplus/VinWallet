using AutoMapper;
using Azure.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Models;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Request;
using VinWallet.Repository.Payload.Response;
using VinWallet.Repository.Utils;

namespace VinWallet.API.Service.Implements
{
    public class AuthService : BaseService<AuthService>, IAuthService
    {
        public AuthService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<AuthService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<LoginResponse> Login(LoginRequest loginRequest)
        {
            Expression<Func<User, bool>> searchFilter = p =>
               p.Username.Equals(loginRequest.Username) &&
               p.Password.Equals(PasswordUtil.HashPassword(loginRequest.Password));

            User user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: searchFilter,include: x=> x.Include(x => x.Role));
            if (user == null) throw new BadHttpRequestException(MessageConstant.LoginMessage.InvalidUsernameOrPassword);
            LoginResponse loginResponse = new LoginResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role.Name,
                Status = user.Status,
            };
            Tuple<string, Guid> guidClaim = new Tuple<string, Guid>("UserId", user.Id);
            var token = JwtUtil.GenerateJwtToken(user, guidClaim);
            loginResponse.AccessToken = token.AccessToken;
            loginResponse.RefreshToken = token.RefreshToken;
            return loginResponse;
        }

        public async Task<LoginResponse> RefreshToken(RefreshTokenRequest refreshTokenRequest)
        {
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(refreshTokenRequest.UserId), include: x => x.Include(x => x.Role));
            if (user == null) throw new BadHttpRequestException(MessageConstant.UserMessage.UserNotFound);
            var token = JwtUtil.RefreshToken(refreshTokenRequest);
            LoginResponse loginResponse = new LoginResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role.Name,
                Status = user.Status,
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken
            };
            return loginResponse;
        }
    }
}
