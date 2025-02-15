using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Response.UserResponse;
using VinWallet.Repository.Payload.Response.WalletResponse;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class UserController : BaseController<UserController>
    {
        private readonly IUserService _userService;
        private readonly IWalletService _walletService;
        public UserController(ILogger<UserController> logger, IUserService userService, IWalletService walletService) : base(logger)
        {
            _userService = userService;
            _walletService = walletService;
        }

        [HttpPost(ApiEndPointConstant.User.UsersEndpoint)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewUser([FromBody] CreateUserRequest createUserRequest)
        {
            var response = await _userService.CreateUser(createUserRequest);
            if (response == null)
            {
                return Problem($"{MessageConstant.UserMessage.CreateUserFailed}: {createUserRequest.Username}");
            }

            return CreatedAtAction(nameof(CreateNewUser), response);
        }

        [HttpGet(ApiEndPointConstant.User.UserEndpoint)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserById([FromRoute] Guid id)
        {
            var response = await _userService.GetUserById(id);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.User.WalletsOfUserEndpoint)]
        [ProducesResponseType(typeof(IPaginate<WalletResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWalletsOfUser([FromRoute] Guid id, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _walletService.GetWalletsOfUser(id, page, size);
            return Ok(response);
        }

    }
}
