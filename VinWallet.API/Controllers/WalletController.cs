using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Payload.Response.WalletResponse;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class WalletController : BaseController<WalletController>
    {
        private readonly IWalletService _walletService;
        public WalletController(ILogger<WalletController> logger, IWalletService walletService) : base(logger)
        {
            _walletService = walletService;
        }

        [HttpGet(ApiEndPointConstant.Wallet.WalletEndpoint)]
        [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWalletById([FromRoute] Guid id)
        {
            var response = await _walletService.GetWalletById(id);
            return Ok(response);
        }
    }
}
