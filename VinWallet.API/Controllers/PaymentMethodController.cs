using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Payload.Response.PaymentMethodResponse;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class PaymentMethodController : BaseController<PaymentMethodController>
    {
        private readonly IPaymentMethodService _paymentMethodService;
        public PaymentMethodController(ILogger<PaymentMethodController> logger, IPaymentMethodService paymentMethodService) : base(logger)
        {
            _paymentMethodService = paymentMethodService;
        }

        [HttpGet(ApiEndPointConstant.PaymentMethod.PaymentMethodsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<PaymentMethodResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? orderBy, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var paymentMethod = await _paymentMethodService.GetAll(search, orderBy, page, size);
            return Ok(paymentMethod);
        }


    }
}
