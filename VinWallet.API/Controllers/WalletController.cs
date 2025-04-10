using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Validators;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.WalletRequest;
using VinWallet.Repository.Payload.Response.TransactionResponse;
using VinWallet.Repository.Payload.Response.UserResponse;
using VinWallet.Repository.Payload.Response.WalletResponse;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class WalletController : BaseController<WalletController>
    {
        private readonly IWalletService _walletService;
        private readonly IUserService _userService;
        private readonly ITransactionService _transactionService;
        public WalletController(ILogger<WalletController> logger, IWalletService walletService, IUserService userService, ITransactionService transactionService) : base(logger)
        {
            _walletService = walletService;
            _userService = userService;
            _transactionService = transactionService;
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

        [HttpGet(ApiEndPointConstant.Wallet.GetTransactionByWalletId)]
        [ProducesResponseType(typeof(IPaginate<GetTransactionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionByWalletId([FromRoute] Guid id, [FromQuery] string? search, [FromQuery] string? orderBy, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _transactionService.GetTransactionByWalletId(id, search, orderBy, page, size);
            return Ok(response);
        }

        [HttpPatch(ApiEndPointConstant.Wallet.ChangeOwner)]
        [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeOwner([FromRoute] Guid id, [FromRoute] Guid userId)
        {
            var response = await _walletService.UpdateOwnerId(id, userId);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Wallet.GetWalletContributionStatistics)]
        [ProducesResponseType(typeof(WalletContributionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWalletContributionStatistics([FromRoute] Guid id, [FromQuery] int days = 30)
        {
            try
            {
                var response = await _walletService.GetWalletContributionStatistics(id, days);
                return Ok(response);
            }
            catch (BadHttpRequestException ex)
            {
                return Problem(ex.Message, statusCode: ex.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet contribution statistics");
                return Problem(MessageConstant.WalletMessage.GetWalletContributionStatisticsFailed);
            }
        }

        [HttpGet(ApiEndPointConstant.Wallet.WalletsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<WalletResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllWallet([FromQuery] string? search, [FromQuery] string? orderBy, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _walletService.GetAllWallets(search, orderBy, page, size);
            return Ok(response);
        }

        [HttpPut(ApiEndPointConstant.Wallet.WalletDissolution)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> WalletDissolution([FromRoute] Guid id)
        {
            var response = await _walletService.WalletDissolution(id);
            if (!response)
            {
                return Problem(MessageConstant.WalletMessage.WalletDissolutionFailed);
            }
            return Ok(response);
        }
        [HttpPut(ApiEndPointConstant.Wallet.TransferFromSharedToPersonal)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> TransferFromSharedToPersonal(TransferRequest transferRequest)
        {
            var response = await _walletService.TransferFromSharedToPersonal(transferRequest.SharedWalletId, transferRequest.PersonalWalletId, transferRequest.Amount);
            if (!response)
            {
                return Problem(MessageConstant.WalletMessage.TransferFailed);
            }
            return Ok(response);
        }
        [HttpPut(ApiEndPointConstant.Wallet.TransferFromPersonalToShared)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> TransferFromPersonalToShared(TransferRequest transferRequest)
        {
            var response = await _walletService.TransferFromPersonalToShared(transferRequest.PersonalWalletId, transferRequest.SharedWalletId, transferRequest.Amount);
            if (!response)
            {
                return Problem(MessageConstant.WalletMessage.TransferFailed);
            }
            return Ok(response);
        }

    }
}
