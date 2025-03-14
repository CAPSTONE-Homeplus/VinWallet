using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Payload.Request.WalletRequest;
using VinWallet.Repository.Payload.Response.WalletResponse;

namespace VinWallet.API.Service.Interfaces
{
    public interface IWalletService
    {
        Task<WalletResponse> CreateWallet(CreateWalletRequest createWalletRequest);

        Task<WalletResponse> GetWalletById(Guid id);

        Task<IPaginate<WalletResponse>> GetWalletsOfUser(Guid id, int page, int size);

        Task<bool> ConnectWalletToUser(Guid? userId, Guid walletId);

        Task CreatePersionalWallet(Guid userId);

        Task<WalletResponse> CreateShareWallet(Guid userId);

        Task<bool> UpdateWalletBalance(Guid walletId, string amount, TransactionCategoryEnum.TransactionCategory transactionCategory);

        Task<bool> DeleteUserWallet(Guid userId, Guid walletId);
        Task<WalletResponse> UpdateOwnerId(Guid walletId, Guid userId);
        Task<WalletContributionResponse> GetWalletContributionStatistics(Guid walletId, int days);
    }
}
