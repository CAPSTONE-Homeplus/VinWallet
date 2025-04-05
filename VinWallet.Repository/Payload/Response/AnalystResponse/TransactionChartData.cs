using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.AnalystResponse
{
    public class TransactionChartData
    {
        public string TimeLabel { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionTypeDistribution
    {
        public string Type { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionStatusDistribution
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class WalletTransactionComparison
    {
        public Guid WalletId { get; set; }
        public string WalletName { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalSpending { get; set; }
    }

    public class SpendingDepositTrend
    {
        public string TimeLabel { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal SpendingAmount { get; set; }
    }
}
