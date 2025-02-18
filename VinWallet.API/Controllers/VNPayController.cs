using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.Repository.Constants;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class VNPayController : BaseController<VNPayController>
    {
        private readonly IVNPayService _vnpayService;
        public VNPayController(ILogger<VNPayController> logger, IVNPayService vnpayService) : base(logger)
        {
            _vnpayService = vnpayService;
        }
        [HttpGet(ApiEndPointConstant.VNPay.Payment)]
        public string Payment(string amount, string infor)
        {
            var paymentUrl = _vnpayService.GeneratePaymentUrl(amount, infor);
            return paymentUrl;
        }

        [HttpGet(ApiEndPointConstant.VNPay.PaymentConfirm)]
        public async Task<IActionResult> PaymentConfirm()
        {
            if (Request.QueryString.HasValue)
            {
                bool isSuccess = await _vnpayService.ProcessPaymentConfirmation(Request.QueryString.Value);
                if (isSuccess)
                {
                    return Ok("Payment successful");
                }
                return StatusCode(402, "Payment failed");
            }

            return StatusCode(500, "Invalid request");
        }
    }
}
