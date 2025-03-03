using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace VinWallet.API.Hubs
{
    public class UserProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
