using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.TransactionRequest;
using VinWallet.Repository.Payload.Response.TransactionResponse;

namespace VinWallet.API.Service.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransaction(CreateTransactionRequest createTransactionRequest);
        Task<bool> ProcessPayment(CreateTransactionRequest createTransactionRequest);

        Task<bool> UpdateTransactionStatus(Guid id, TransactionEnum.TransactionStatus transactionStatus);
    }
}
