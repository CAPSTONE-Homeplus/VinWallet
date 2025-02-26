using Microsoft.AspNetCore.SignalR;

namespace VinWallet.API.Hubs
{
    public class UserProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.Identity?.Name;
        }
    }
}
