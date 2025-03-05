using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Service.RabbitMQ;
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
        private readonly ISignalRHubService _signalRHubService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly RabbitMQPublisher _rabbitMQPublisher;

        public AuthService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<AuthService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, ISignalRHubService signalRHubService, IBackgroundJobClient backgroundJobClient, RabbitMQPublisher rabbitMQPublisher) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _signalRHubService = signalRHubService;
            _backgroundJobClient = backgroundJobClient;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public async Task<LoginResponse> Login(LoginRequest loginRequest)
        {
            // Kiểm tra nếu username có tồn tại trước
            Expression<Func<Domain.Models.User, bool>> usernameFilter = p => p.Username.Equals(loginRequest.Username);
            Domain.Models.User existingUser = await _unitOfWork.GetRepository<Domain.Models.User>().SingleOrDefaultAsync(predicate: usernameFilter);

            if (existingUser == null)
            {
                throw new BadHttpRequestException(MessageConstant.LoginMessage.UserNotRegistered); 
            }

            Expression<Func<Domain.Models.User, bool>> searchFilter = p =>
                p.Username.Equals(loginRequest.Username) &&
                p.Password.Equals(PasswordUtil.HashPassword(loginRequest.Password));

            Domain.Models.User user = await _unitOfWork.GetRepository<Domain.Models.User>().SingleOrDefaultAsync(predicate: searchFilter, include: x => x.Include(x => x.Role));

            if (user == null)
            {
                throw new BadHttpRequestException(MessageConstant.LoginMessage.InvalidUsernameOrPassword); 
            }

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

            await _rabbitMQPublisher.Publish("add_wallet_member", "vinwallet" ,"Tuan Anh test RabbitMQ");
            return loginResponse;
        }


        public async Task<LoginResponse> RefreshToken(RefreshTokenRequest refreshTokenRequest)
        {
            var user = await _unitOfWork.GetRepository<Domain.Models.User>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(refreshTokenRequest.UserId), include: x => x.Include(x => x.Role));
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

            _backgroundJobClient.Enqueue(() => _signalRHubService.SendNotificationToUser("161e1a9d-8bca-48c6-b52e-9814594f685b", "Tuan Anh test SinalR to User"));
            return loginResponse;
        }
    }
}
