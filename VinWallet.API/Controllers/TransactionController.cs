using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Validators;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.TransactionRequest;
using VinWallet.Repository.Payload.Response.AnalystResponse;
using VinWallet.Repository.Payload.Response.TransactionResponse;

namespace VinWallet.API.Controllers
{
    [ApiController]
    public class TransactionController : BaseController<TransactionController>
    {
        private readonly ITransactionService _transactionService;
        public TransactionController(ILogger<TransactionController> logger, ITransactionService transactionService) : base(logger)
        {
            _transactionService = transactionService;
        }

        //[CustomAuthorize(UserEnum.Role.Leader, UserEnum.Role.Member)]
        //[HttpPost(ApiEndPointConstant.Transaction.TransactionsEndpoint)]
        //[ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest createTransactionRequest)
        //{
        //    var response = await _transactionService.CreateTransaction(createTransactionRequest);
        //    if (response == null)
        //    {
        //        return Problem($"{MessageConstant.TransactionMessage.CreateTransactionFailed}: {createTransactionRequest.Amount}");
        //    }

        //    return CreatedAtAction(nameof(CreateTransaction), response);
        //}

        [CustomAuthorize(UserEnum.Role.Leader, UserEnum.Role.Member)]
        [HttpPost(ApiEndPointConstant.Transaction.TransactionsEndpoint)]
        [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ProcessPayment([FromBody] CreateTransactionRequest request)
        {
            var response = await _transactionService.ProcessPayment(request);
            if (response == null)
            {
                return Problem($"{MessageConstant.TransactionMessage.CreateTransactionFailed}: {request.Amount}");
            }

            return CreatedAtAction(nameof(ProcessPayment), response);

        }

        [HttpGet(ApiEndPointConstant.Transaction.TransactionsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetTransactionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTransactions([FromQuery] string? search, [FromQuery] string? orderBy, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var response = await _transactionService.GetAllTransaction(search, orderBy, page, size);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.TransactionEndpoint)]
        [ProducesResponseType(typeof(GetTransactionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionById(Guid id)
        {
            var response = await _transactionService.GetTransactionById(id);
            return Ok(response);
        }

        
        [HttpGet(ApiEndPointConstant.Transaction.TransactionByTimePeriodEndpoint)]
        [ProducesResponseType(typeof(List<TransactionChartData>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionsByTimePeriod([FromQuery] Guid userId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string groupBy = "day")
        {
            var response = await _transactionService.GetTransactionsByTimePeriod(userId, startDate, endDate, groupBy);
            return Ok(response);
        }

        
        [HttpGet(ApiEndPointConstant.Transaction.TransactionTypeDistributionEndpoint)]
        [ProducesResponseType(typeof(List<TransactionTypeDistribution>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionTypeDistribution([FromQuery] Guid userId)
        {
            var response = await _transactionService.GetTransactionTypeDistribution(userId);
            return Ok(response);
        }

      
        [HttpGet(ApiEndPointConstant.Transaction.TransactionStatusDistributionEndpoint)]
        [ProducesResponseType(typeof(List<TransactionStatusDistribution>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionStatusDistribution([FromQuery] Guid userId)
        {
            var response = await _transactionService.GetTransactionStatusDistribution(userId);
            return Ok(response);
        }

       
        [HttpGet(ApiEndPointConstant.Transaction.CompareWalletTransactionsEndpoint)]
        [ProducesResponseType(typeof(List<WalletTransactionComparison>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CompareWalletTransactions([FromQuery] Guid userId)
        {
            var response = await _transactionService.CompareWalletTransactions(userId);
            return Ok(response);
        }

       
        [HttpGet(ApiEndPointConstant.Transaction.SpendingDepositTrendEndpoint)]
        [ProducesResponseType(typeof(List<SpendingDepositTrend>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSpendingDepositTrend([FromQuery] Guid userId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string interval = "month")
        {
            var response = await _transactionService.GetSpendingDepositTrend(userId, startDate, endDate, interval);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.AdminDashboardOverview)]
        [ProducesResponseType(typeof(AdminDashboardOverview), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdminDashboardOverview([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? type = null)
        {
            var response = await _transactionService.GetAdminDashboardOverview(fromDate, toDate, type);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.AdminTransactionStats)]
        [ProducesResponseType(typeof(AdminTransactionStats), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdminTransactionStats([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? type = null)
        {
            var response = await _transactionService.GetAdminTransactionStats(fromDate, toDate, type);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.DailyTransactionStats)]
        [ProducesResponseType(typeof(List<DailyTransactionStats>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDailyTransactionStats([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? type = null)
        {
            var response = await _transactionService.GetDailyTransactionStatsForAdmin(fromDate, toDate, type);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.TopUsersByTransaction)]
        [ProducesResponseType(typeof(List<TopUserByTransaction>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTopUsersByTransactions([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? type = null, [FromQuery] int limit = 10)
        {
            var response = await _transactionService.GetTopUsersByTransactions(fromDate, toDate, type, limit);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.WalletTypeStats)]
        [ProducesResponseType(typeof(List<WalletTypeStats>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWalletTypeStats([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? type = null)
        {
            var response = await _transactionService.GetWalletTypeStats(fromDate, toDate, type);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.MonthlyTransactionTrend)]
        [ProducesResponseType(typeof(List<MonthlyTransactionTrend>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMonthlyTransactionTrend([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? type = null)
        {
            var response = await _transactionService.GetMonthlyTransactionTrend(fromDate, toDate, type);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.PaymentMethodStats)]
        [ProducesResponseType(typeof(List<PaymentMethodStats>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentMethodStats([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? type = null)
        {
            var response = await _transactionService.GetPaymentMethodStats(fromDate, toDate, type);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Transaction.TransactionCategoryStats)]
        [ProducesResponseType(typeof(List<CategoryStats>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionCategoryStats([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromQuery] string? type = null)
        {
            var response = await _transactionService.GetTransactionCategoryStats(fromDate, toDate, type);
            return Ok(response);
        }

    }
}
