using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VinWallet.Domain.Models;
using VinWallet.Repository.Generic.Interfaces;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;


namespace VinWallet.API.Service
{
    public abstract class BaseService<T> where T : class
    {
        protected IUnitOfWork<VinWalletContext> _unitOfWork;
        protected ILogger<T> _logger;
        protected IMapper _mapper;
        protected IHttpContextAccessor _httpContextAccessor;


        public BaseService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<T> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        protected string GetUsernameFromJwt()
        {
            return _httpContextAccessor?.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }

        protected string GetRoleFromJwt()
        {
            string role = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return role;
        }

        protected string GetUserIdFromJwt()
        {
            string userId = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId;
        }
    }
}
