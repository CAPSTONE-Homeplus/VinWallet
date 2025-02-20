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
                // Xử lý xác nhận thanh toán và nhận kết quả dưới dạng DTO
                var paymentData = await _vnpayService.ProcessPaymentConfirmation(Request.QueryString.Value);

                if (paymentData.IsSuccess)
                {
                    return Ok(new
                    {
                        Message = "Payment successful",
                        TransactionId = paymentData.VnpTransactionNo,
                        OrderId = paymentData.VnpTxnRef,
                        OrderInfo = paymentData.VnpOrderInfo,
                        BankCode = paymentData.VnpTmnCode,
                        SignatureValid = paymentData.IsValidSignature
                    });
                }

                return StatusCode(402, new
                {
                    Message = "Payment failed",
                    TransactionId = paymentData.VnpTransactionNo,
                    OrderId = paymentData.VnpTxnRef,
                    OrderInfo = paymentData.VnpOrderInfo,
                    BankCode = paymentData.VnpTmnCode,
                    SignatureValid = paymentData.IsValidSignature
                });
            }

            return StatusCode(400, new { Message = "Invalid request" });
        }

    }
}
