using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;
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

        Task CreateAndConnectWalletToUser(Guid userId, Role role, Guid roomId);
    }
}
