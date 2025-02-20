using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Enums
{
    public class TransactionEnum
    {
        public enum TransactionStatus
        {
            Pending = 1,
            Success = 2,
            Failed = 3
        }
        public enum TransactionType
        {
            Deposit = 1,
            Spending = 2,
        }
    }
}
