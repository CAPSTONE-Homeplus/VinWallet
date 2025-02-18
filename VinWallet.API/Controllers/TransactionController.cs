using Microsoft.AspNetCore.Mvc;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Validators;
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

    }
}
