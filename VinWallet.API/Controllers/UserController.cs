using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Implements;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Validators;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.UserRequest;
using VinWallet.Repository.Payload.Response.TransactionResponse;
using VinWallet.Repository.Payload.Response.UserResponse;
using VinWallet.Repository.Payload.Response.WalletResponse;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class UserController : BaseController<UserController>
    {
        private readonly IUserService _userService;
        private readonly IWalletService _walletService;
        private readonly ITransactionService _transactionService;
        public UserController(ILogger<UserController> logger, IUserService userService, IWalletService walletService, ITransactionService transactionService) : base(logger)
        {
            _userService = userService;
            _walletService = walletService;
            _transactionService = transactionService;
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

        [CustomAuthorize(UserEnum.Role.Member, UserEnum.Role.Leader)]
        [HttpGet(ApiEndPointConstant.User.WalletsOfUserEndpoint)]
        [ProducesResponseType(typeof(IPaginate<WalletResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWalletsOfUser([FromRoute] Guid id, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _walletService.GetWalletsOfUser(id, page, size);
            return Ok(response);
        }

        [CustomAuthorize(UserEnum.Role.Leader, UserEnum.Role.Member)]
        [HttpGet(ApiEndPointConstant.User.TransactionsOfUserEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetTransactionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionByUserId(Guid id, [FromQuery] string? search, [FromQuery] string? orderBy, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _transactionService.GetTransactionByUserId(id, search, orderBy, page, size);
            return Ok(response);
        }

        [CustomAuthorize(UserEnum.Role.Leader, UserEnum.Role.Member)]
        [HttpGet(ApiEndPointConstant.User.TransactionsOfUserEndpointByWalletId)]
        [ProducesResponseType(typeof(IPaginate<GetTransactionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionByUserIdAndWalletId(Guid id, Guid walletId, [FromQuery] string? search, [FromQuery] string? orderBy, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _transactionService.GetTransactionByUserIdAndWalletId(id, walletId, search, orderBy, page, size);
            return Ok(response);
        }

        [HttpPost(ApiEndPointConstant.User.CreateShareWallet)]
        [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateShareWallet([FromRoute] Guid id)
        {
            var response = await _walletService.CreateShareWallet(id);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.User.GetUserByPhoneNumber)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserByPhoneNumber([FromRoute] string phoneNumber)
        {
            var response = await _userService.GetUserByPhoneNumber(phoneNumber);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.User.CheckUserInfo)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckUserInfo([FromQuery] string? phoneNumber, [FromQuery] string? email, [FromQuery] string? username)
        {
            var response = await _userService.CheckUserInfo(phoneNumber, email, username);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.User.UsersEndpoint)]
        [ProducesResponseType(typeof(IPaginate<UserResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? search, [FromQuery] string? orderBy, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _userService.GetAllUsers(search, orderBy, page, size);
            return Ok(response);
        }
    }
}
