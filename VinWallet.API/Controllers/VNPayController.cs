using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
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

                // Tạo HTML content
                string htmlContent = GeneratePaymentResultHtml(paymentData);

                // Trả về ContentResult với Content-Type là text/html
                return Content(htmlContent, "text/html", Encoding.UTF8);
            }

            string errorHtml = GenerateErrorHtml("Yêu cầu không hợp lệ");
            return Content(errorHtml, "text/html", Encoding.UTF8);
        }

        // Tạo nội dung HTML cho kết quả thanh toán
        private string GeneratePaymentResultHtml(dynamic paymentData)
        {
            string statusClass = paymentData.IsSuccess ? "success-header" : "failed-header";
            string statusIcon = paymentData.IsSuccess ? "✓" : "✗";
            string statusMessage = paymentData.IsSuccess ? "Thanh toán thành công" : "Thanh toán thất bại";

            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kết quả thanh toán</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f8f9fa;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            padding: 20px;
        }}
        
        .payment-container {{
            background-color: white;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            padding: 30px;
            width: 100%;
            max-width: 500px;
            text-align: center;
        }}
        
        .success-header {{
            color: #28a745;
            border-bottom: 1px solid #e9ecef;
            padding-bottom: 15px;
            margin-bottom: 20px;
        }}
        
        .failed-header {{
            color: #dc3545;
            border-bottom: 1px solid #e9ecef;
            padding-bottom: 15px;
            margin-bottom: 20px;
        }}
        
        .payment-icon {{
            font-size: 48px;
            margin-bottom: 20px;
        }}
        
        .payment-details {{
            text-align: left;
            margin: 20px 0;
        }}
        
        .details-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
            padding-bottom: 10px;
            border-bottom: 1px solid #f0f0f0;
        }}
        
        .details-label {{
            font-weight: 600;
            color: #6c757d;
        }}
        
        .details-value {{
            font-weight: 500;
        }}
        
        .return-button {{
            background-color: #007bff;
            color: white;
            border: none;
            border-radius: 4px;
            padding: 10px 20px;
            font-size: 16px;
            cursor: pointer;
            transition: background-color 0.3s;
            margin-top: 20px;
        }}
        
        .return-button:hover {{
            background-color: #0069d9;
        }}

        @media (max-width: 768px) {{
            .payment-container {{
                padding: 15px;
            }}
            
            .details-row {{
                flex-direction: column;
                gap: 5px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""payment-container"">
        <div class=""{statusClass}"">
            <div class=""payment-icon"">{statusIcon}</div>
            <h1>{statusMessage}</h1>
        </div>
        
        <div class=""payment-details"">
            <div class=""details-row"">
                <span class=""details-label"">Mã giao dịch:</span>
                <span class=""details-value"">{paymentData.VnpTransactionNo}</span>
            </div>
            <div class=""details-row"">
                <span class=""details-label"">Mã đơn hàng:</span>
                <span class=""details-value"">{paymentData.VnpTxnRef}</span>
            </div>
            <div class=""details-row"">
                <span class=""details-label"">Thông tin đơn hàng:</span>
                <span class=""details-value"">{paymentData.VnpOrderInfo}</span>
            </div>
            <div class=""details-row"">
                <span class=""details-label"">Ngân hàng:</span>
                <span class=""details-value"">{paymentData.VnpTmnCode}</span>
            </div>
            <div class=""details-row"">
                <span class=""details-label"">Tính toàn vẹn:</span>
                <span class=""details-value"">{(paymentData.IsValidSignature ? "Hợp lệ" : "Không hợp lệ")}</span>
            </div>
        </div>
        
        <button class=""return-button"" onclick=""window.location.href='/'"">Trở về trang chủ</button>
    </div>
</body>
</html>";
        }

        // Tạo nội dung HTML cho trang lỗi
        private string GenerateErrorHtml(string errorMessage)
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Lỗi</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f8f9fa;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            padding: 20px;
        }}
        
        .error-container {{
            background-color: white;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            padding: 30px;
            width: 100%;
            max-width: 500px;
            text-align: center;
        }}
        
        .error-header {{
            color: #dc3545;
            border-bottom: 1px solid #e9ecef;
            padding-bottom: 15px;
            margin-bottom: 20px;
        }}
        
        .error-icon {{
            font-size: 48px;
            margin-bottom: 20px;
        }}
        
        .return-button {{
            background-color: #007bff;
            color: white;
            border: none;
            border-radius: 4px;
            padding: 10px 20px;
            font-size: 16px;
            cursor: pointer;
            transition: background-color 0.3s;
            margin-top: 20px;
        }}
        
        .return-button:hover {{
            background-color: #0069d9;
        }}
    </style>
</head>
<body>
    <div class=""error-container"">
        <div class=""error-header"">
            <div class=""error-icon"">⚠️</div>
            <h1>Lỗi</h1>
        </div>
        
        <p>{errorMessage}</p>
        
       
    </div>
</body>
</html>";
        }
    }
}
