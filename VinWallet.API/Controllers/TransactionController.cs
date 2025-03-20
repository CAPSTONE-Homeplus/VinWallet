using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Validators;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.TransactionRequest;
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

    }
}
