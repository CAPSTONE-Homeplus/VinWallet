using AutoMapper;
using Azure.Core;
using System.Net;
using System.Web;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Service.RabbitMQ.Message;
using VinWallet.API.Service.RabbitMQ;
using VinWallet.API.VnPay;
using VinWallet.Domain.Models;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Response.VnPayDto;
using VinWallet.Repository.Utils;
using static VinWallet.Repository.Constants.ApiEndPointConstant;

namespace VinWallet.API.Service.Implements
{
    public class VNPayService : BaseService<VNPayService>, IVNPayService
    {
        private readonly VNPaySettings _vnPaySettings;
        private readonly IWalletService _walletService;
        private readonly RabbitMQPublisher _rabbitMQPublisher;


        public VNPayService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<VNPayService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, VNPaySettings vNPaySettings, IWalletService walletService, RabbitMQPublisher rabbitMQPublisher) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _vnPaySettings = vNPaySettings;
            _walletService = walletService;
            _rabbitMQPublisher = rabbitMQPublisher;
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

        public async Task<VnPayPaymentConfirmDto> ProcessPaymentConfirmation(string queryString)
        {
            var paymentData = new VnPayPaymentConfirmDto();

            if (!string.IsNullOrEmpty(queryString))
            {
                var json = HttpUtility.ParseQueryString(queryString);

                paymentData.VnpTxnRef = json["vnp_TxnRef"];
                paymentData.VnpOrderInfo = json["vnp_OrderInfo"];
                paymentData.VnpTransactionNo = json["vnp_TransactionNo"];
                paymentData.VnpResponseCode = json["vnp_ResponseCode"];
                paymentData.VnpSecureHash = json["vnp_SecureHash"];
                paymentData.VnpTmnCode = json["vnp_TmnCode"];

                if (!long.TryParse(paymentData.VnpTxnRef, out long orderId) ||
                    !long.TryParse(paymentData.VnpTransactionNo, out long vnpayTranId))
                {
                    paymentData.IsValidSignature = false;
                    paymentData.IsSuccess = false;
                    return paymentData;
                }

                int pos = queryString.IndexOf("&vnp_SecureHash");
                string dataToVerify = queryString.Substring(1, pos - 1);

                paymentData.IsValidSignature = ValidateSignature(dataToVerify, paymentData.VnpSecureHash, _vnPaySettings.HashSecret);

                paymentData.IsSuccess = paymentData.IsValidSignature &&
                                        _vnPaySettings.TmnCode == paymentData.VnpTmnCode &&
                                        paymentData.VnpResponseCode == "00";

                var transaction = await _unitOfWork.GetRepository<VinWallet.Domain.Models.Transaction>().SingleOrDefaultAsync(predicate: x => x.Id.ToString().Equals(paymentData.VnpOrderInfo));

                if (paymentData.IsSuccess && transaction != null)
                {
                   
                    var success = await _walletService.UpdateWalletBalance(transaction.WalletId ?? Guid.Empty, transaction.Amount, TransactionCategoryEnum.TransactionCategory.Deposit);
                    var status = success ?
                    TransactionEnum.TransactionStatus.Success :
                    TransactionEnum.TransactionStatus.Failed;

                    await UpdateTransactionStatus(transaction.Id, status);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _rabbitMQPublisher.Publish("order_payment_successed", new OrderPaymentSuccessMessage
                            {
                               TransactionId = transaction.Id,
                               WalletId = transaction.WalletId,
                                Amount = transaction.Amount,
                                Timestamp = transaction.CreatedAt,
                                OrderId = transaction.OrderId
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ RabbitMQ publish failed: {ex.Message}");
                        }
                    });

                }
                else
                {
                    await UpdateTransactionStatus(transaction.Id, TransactionEnum.TransactionStatus.Failed);
                }
            }
            else
            {
                paymentData.IsValidSignature = false;
                paymentData.IsSuccess = false;
            }

            return paymentData;
        }
        public async Task<bool> UpdateTransactionStatus(Guid id, TransactionEnum.TransactionStatus transactionStatus)
        {
            if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.TransactionMessage.EmptyTransactionId);
            var transaction = await _unitOfWork.GetRepository<VinWallet.Domain.Models.Transaction>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(id));
            if (transaction == null) throw new BadHttpRequestException(MessageConstant.TransactionMessage.TransactionNotFound);
            transaction.Status = transactionStatus.ToString();
            transaction.UpdatedAt = DateTime.UtcNow.AddHours(7);
            _unitOfWork.GetRepository<VinWallet.Domain.Models.Transaction>().UpdateAsync(transaction);
            if (await _unitOfWork.CommitAsync() <= 0) return false;
            return true;
        }

        private bool ValidateSignature(string rspraw, string inputHash, string secretKey)
        {
            string myChecksum = VNPayHelper.HmacSHA512(secretKey, rspraw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
