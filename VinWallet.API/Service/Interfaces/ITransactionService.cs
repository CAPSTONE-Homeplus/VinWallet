using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.TransactionRequest;
using VinWallet.Repository.Payload.Response.TransactionResponse;
using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;

namespace VinWallet.API.Service.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransaction(CreateTransactionRequest createTransactionRequest);
        Task<TransactionResponse> ProcessPayment(CreateTransactionRequest createTransactionRequest);

        Task<bool> UpdateTransactionStatus(Transaction transaction, TransactionEnum.TransactionStatus transactionStatus);

        Task<IPaginate<GetTransactionResponse>> GetTransactionByUserId(Guid userId, string? search, string? orderBy, int page, int size);
        Task<IPaginate<GetTransactionResponse>> GetTransactionByUserIdAndWalletId(Guid userId, Guid walletId, string? search, string? orderBy, int page, int size);
    }
}
