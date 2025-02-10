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
            Expression<Func<Account, bool>> searchFilter = p =>
               p.Username.Equals(loginRequest.Username) &&
               p.Password.Equals(PasswordUtil.HashPassword(loginRequest.Password));

            Account account = await _unitOfWork.GetRepository<Account>().SingleOrDefaultAsync(predicate: searchFilter,include: x => x.Include(x => x.User).Include(x => x.Role));
            if (account == null) throw new BadHttpRequestException(MessageConstant.LoginMessage.InvalidUsernameOrPassword);
            LoginResponse loginResponse = new LoginResponse
            {
                AccountId = account.Id,
                FullName = account.User.FullName,
                ImageUrl = account.ImageUrl,
                Role = account.Role.Name,
                Status = account.Status,
            };
            Tuple<string, Guid> guidClaim = new Tuple<string, Guid>("UserId", account.User.Id);
            var token = JwtUtil.GenerateJwtToken(account, guidClaim);
            loginResponse.AccessToken = token.AccessToken;
            loginResponse.RefreshToken = token.RefreshToken;
            return loginResponse;
        }
    }
}
