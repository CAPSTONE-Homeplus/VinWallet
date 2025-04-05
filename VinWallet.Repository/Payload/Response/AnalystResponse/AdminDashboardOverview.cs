using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.AnalystResponse
{
    public class AdminDashboardOverview
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string TransactionType { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalUsers { get; set; }
        public int TotalWallets { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalSpending { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public int PendingTransactions { get; set; }
    }

    // Thống kê giao dịch theo khoảng thời gian
    public class AdminTransactionStats
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TransactionType { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public int DepositCount { get; set; }
        public int SpendingCount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal SpendingAmount { get; set; }
        public List<StatusCount> StatusDistribution { get; set; }
    }

    public class StatusCount
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    // Thống kê giao dịch theo ngày
    public class DailyTransactionStats
    {
        public DateTime Date { get; set; }
        public string TransactionType { get; set; }
        public int TransactionCount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal SpendingAmount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
    }

    // Top người dùng có nhiều giao dịch
    public class TopUserByTransaction
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string TransactionType { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal SpendingAmount { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }

    // Thống kê theo loại ví
    public class WalletTypeStats
    {
        public string WalletType { get; set; }
        public string TransactionType { get; set; }
        public int WalletCount { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal SpendingAmount { get; set; }
    }

    // Xu hướng giao dịch theo tháng
    public class MonthlyTransactionTrend
    {
        public string YearMonth { get; set; }
        public string TransactionType { get; set; }
        public int TransactionCount { get; set; }
        public int NewUserCount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal SpendingAmount { get; set; }
        public double SuccessRate { get; set; }
    }

    // Thống kê phương thức thanh toán
    public class PaymentMethodStats
    {
        public Guid PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; }
        public string TransactionType { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public double SuccessRate { get; set; }
    }

    // Thống kê danh mục giao dịch
    public class CategoryStats
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string TransactionType { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }
}
