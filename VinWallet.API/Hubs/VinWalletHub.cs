using Microsoft.AspNetCore.SignalR;

namespace VinWallet.API.Hubs
{
    public class VinWalletHub : Hub
    {
        public async Task SendTransactionNotification(string leaderId, string message)
        {
            await Clients.User(leaderId).SendAsync("ReceiveTransactionNotification", message);
        }
    }
}
