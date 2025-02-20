using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.TransactionRequest;
using VinWallet.Repository.Payload.Response.TransactionResponse;
using VinWallet.Domain.Models;

namespace VinWallet.API.Service.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransaction(CreateTransactionRequest createTransactionRequest);
        Task<TransactionResponse> ProcessPayment(CreateTransactionRequest createTransactionRequest);

        Task<bool> UpdateTransactionStatus(Transaction transaction, TransactionEnum.TransactionStatus transactionStatus);
    }
}
