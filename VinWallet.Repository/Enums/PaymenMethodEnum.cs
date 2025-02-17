using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Enums
{
    public class PaymenMethodEnum
    {
        public enum PaymentMethodEnum
        {
            Cash = 1,
            Bank = 2,
            Momo = 3,
            ZaloPay = 4,
            Visa = 5,
            Master = 6,
            Wallet = 7,
            Other = 8
        }

        public enum PaymentMethodStatusEnum
        {
            Active = 1,
            Inactive = 2
        }
    }
}
