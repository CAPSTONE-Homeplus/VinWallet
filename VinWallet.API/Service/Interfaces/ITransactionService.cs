using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.TransactionRequest;
using VinWallet.Repository.Payload.Response.TransactionResponse;
using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Payload.Response.AnalystResponse;

namespace VinWallet.API.Service.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransaction(CreateTransactionRequest createTransactionRequest);
        Task<TransactionResponse> ProcessPayment(CreateTransactionRequest createTransactionRequest);

        Task<bool> UpdateTransactionStatus(Transaction transaction, TransactionEnum.TransactionStatus transactionStatus);

        Task<IPaginate<GetTransactionResponse>> GetTransactionByUserId(Guid userId, string? search, string? orderBy, int page, int size);
        Task<IPaginate<GetTransactionResponse>> GetTransactionByUserIdAndWalletId(Guid userId, Guid walletId, string? search, string? orderBy, int page, int size);
        Task<IPaginate<GetTransactionResponse>> GetTransactionByWalletId(Guid walletId, string? search, string? orderBy, int page, int size);
        Task<GetTransactionResponse> GetTransactionById(Guid transactionId);
        Task<IPaginate<GetTransactionResponse>> GetAllTransaction(string? search, string? orderBy, int page, int size);
        Task<List<TransactionChartData>> GetTransactionsByTimePeriod(Guid userId, DateTime startDate, DateTime endDate, string groupBy = "day");
        Task<List<TransactionTypeDistribution>> GetTransactionTypeDistribution(Guid userId);
        Task<List<TransactionStatusDistribution>> GetTransactionStatusDistribution(Guid userId);
        Task<List<WalletTransactionComparison>> CompareWalletTransactions(Guid userId);
        Task<List<SpendingDepositTrend>> GetSpendingDepositTrend(Guid userId, DateTime startDate, DateTime endDate, string interval = "month");
        Task<AdminDashboardOverview> GetAdminDashboardOverview(DateTime fromDate, DateTime toDate, string type = null);
        Task<AdminTransactionStats> GetAdminTransactionStats(DateTime fromDate, DateTime toDate, string type = null);
        Task<List<DailyTransactionStats>> GetDailyTransactionStatsForAdmin(DateTime fromDate, DateTime toDate, string type = null);
        Task<List<TopUserByTransaction>> GetTopUsersByTransactions(DateTime fromDate, DateTime toDate, string type = null, int limit = 10);
        Task<List<WalletTypeStats>> GetWalletTypeStats(DateTime fromDate, DateTime toDate, string type = null);
        Task<List<MonthlyTransactionTrend>> GetMonthlyTransactionTrend(DateTime fromDate, DateTime toDate, string type = null);
        Task<List<PaymentMethodStats>> GetPaymentMethodStats(DateTime fromDate, DateTime toDate, string type = null);
        Task<List<CategoryStats>> GetTransactionCategoryStats(DateTime fromDate, DateTime toDate, string type = null);


    }
}
