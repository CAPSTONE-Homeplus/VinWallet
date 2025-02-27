using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Validators;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.WalletRequest;
using VinWallet.Repository.Payload.Response.UserResponse;
using VinWallet.Repository.Payload.Response.WalletResponse;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class WalletController : BaseController<WalletController>
    {
        private readonly IWalletService _walletService;
        private readonly IUserService _userService;
        public WalletController(ILogger<WalletController> logger, IWalletService walletService, IUserService userService) : base(logger)
        {
            _walletService = walletService;
            _userService = userService;
        }

        [CustomAuthorize(UserEnum.Role.Leader, UserEnum.Role.Member)]
        [HttpGet(ApiEndPointConstant.Wallet.WalletEndpoint)]
        [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWalletById([FromRoute] Guid id)
        {
            var response = await _walletService.GetWalletById(id);
            return Ok(response);
        }



        [HttpPost(ApiEndPointConstant.Wallet.InviteMemberEndpoint)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> InviteMemberToWallet([FromBody] ConnectWalletToUserRequest request)
        {
            var response = await _walletService.ConnectWalletToUser(request.UserId, request.WalletId);
            if (!response)
            {
                return Problem($"{MessageConstant.WalletMessage.InviteMemberFailed}: {request.UserId}");
            }
            return CreatedAtAction(nameof(InviteMemberToWallet), response);
        }

        [HttpGet(ApiEndPointConstant.Wallet.GetUserInSharedWallet)]
        [ProducesResponseType(typeof(IPaginate<UserResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserByShareWalletId([FromRoute] Guid id, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _userService.GetAllUserByShareWalletId(id, page, size);
            return Ok(response);
        }

        [HttpDelete(ApiEndPointConstant.Wallet.DeleteUserWallet)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteUserWallet([FromRoute] Guid userId, [FromRoute] Guid id)
        {
            var response = await _walletService.DeleteUserWallet(userId, id);
            if (!response)
            {
                return Problem($"{MessageConstant.WalletMessage.DeleteUserWalletFailed}: {userId}");
            }
            return Ok(response);
        }
    }
}
