using AutoMapper;
using System.Net;
using System.Web;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.VnPay;
using VinWallet.Domain.Models;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Utils;

namespace VinWallet.API.Service.Implements
{
    public class VNPayService : BaseService<VNPayService>, IVNPayService
    {
        private readonly VNPaySettings _vnPaySettings;

        public VNPayService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<VNPayService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, VNPaySettings vNPaySettings) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _vnPaySettings = vNPaySettings;
        }

        public string GeneratePaymentUrl(string amount, string infor)
        {
            string orderInfo = DateTime.Now.Ticks.ToString();
            string hostName = Dns.GetHostName();
            string clientIPAddress = Dns.GetHostAddresses(hostName)[0].ToString();

            VNPayHelper pay = new VNPayHelper();
            amount += "00";

            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", _vnPaySettings.TmnCode);
            pay.AddRequestData("vnp_Amount", amount);
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", clientIPAddress);
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", infor);
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", _vnPaySettings.ReturnUrl);
            pay.AddRequestData("vnp_TxnRef", orderInfo);
            string paymentUrl = pay.CreateRequestUrl(_vnPaySettings.Url, _vnPaySettings.HashSecret);
            return paymentUrl;
        }

        public async Task<bool> ProcessPaymentConfirmation(string queryString)
        {
            if (!string.IsNullOrEmpty(queryString))
            {
                var json = HttpUtility.ParseQueryString(queryString);

                long orderId = Convert.ToInt64(json["vnp_TxnRef"]);
                string orderInfo = json["vnp_OrderInfo"]?.ToString();
                long vnpayTranId = Convert.ToInt64(json["vnp_TransactionNo"]);
                string vnp_ResponseCode = json["vnp_ResponseCode"]?.ToString();
                string vnp_SecureHash = json["vnp_SecureHash"]?.ToString();
                var pos = queryString.IndexOf("&vnp_SecureHash");

                bool checkSignature = ValidateSignature(queryString.Substring(1, pos - 1), vnp_SecureHash, _vnPaySettings.HashSecret);

                if (checkSignature && _vnPaySettings.TmnCode == json["vnp_TmnCode"]?.ToString())
                {
                    return vnp_ResponseCode == "00";
                }
            }

            return false;
        }

        private bool ValidateSignature(string rspraw, string inputHash, string secretKey)
        {
            string myChecksum = VNPayHelper.HmacSHA512(secretKey, rspraw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
