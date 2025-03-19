using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Payload.Request;
using VinWallet.Repository.Payload.Response;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class AuthController : BaseController<AuthController>
    {
        private readonly IAuthService _authService;
        public AuthController(ILogger<AuthController> logger, IAuthService authService) : base(logger)
        {
            _authService = authService;
        }

        [HttpPost(ApiEndPointConstant.Authentication.Login)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest loginRequest)
        {
            var response = await _authService.Login(loginRequest);
            return Ok(response);
        }

        [HttpPost(ApiEndPointConstant.Authentication.RefreshToken)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            var response = await _authService.RefreshToken(refreshTokenRequest);
            return Ok(response);
        }

        [HttpPost(ApiEndPointConstant.Authentication.AdminLogin)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginRequest loginRequest)
        {
            var response = await _authService.LoginAdmin(loginRequest);
            return Ok(response);
        }
    }
}
