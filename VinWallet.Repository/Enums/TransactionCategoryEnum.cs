using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Enums
{
    public class TransactionCategoryEnum
    {
        public enum TransactionCategory
        {
            Deposit = 1,
            Spending = 2,
            Refund = 3,
            Withdraw = 4
        }
    }
}
