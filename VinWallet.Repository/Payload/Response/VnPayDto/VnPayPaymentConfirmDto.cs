using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.VnPayDto
{
    public class VnPayPaymentConfirmDto
    {
        public long VnpAmount { get; set; }
        public string VnpBankCode { get; set; }
        public string VnpBankTranNo { get; set; }
        public string VnpCardType { get; set; }
        public string VnpOrderInfo { get; set; }
        public string VnpPayDate { get; set; }
        public string VnpResponseCode { get; set; }
        public string VnpTmnCode { get; set; }
        public string VnpTransactionNo { get; set; }
        public string VnpTransactionStatus { get; set; }
        public string VnpTxnRef { get; set; }
        public string VnpSecureHash { get; set; }

        public bool IsValidSignature { get; set; } // Kiểm tra chữ ký hợp lệ hay không
        public bool IsSuccess { get; set; } // Giao dịch có thành công không
    }

}
