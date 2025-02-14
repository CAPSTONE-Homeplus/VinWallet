using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Response.UserResponse;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class UserController : BaseController<UserController>
    {
        private readonly IUserService _userService;
        public UserController(ILogger<UserController> logger, IUserService userService) : base(logger)
        {
            _userService = userService;
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

    }
}
