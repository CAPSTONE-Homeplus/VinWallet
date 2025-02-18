namespace VinWallet.API.Service.Interfaces
{
    public interface ISignalRHubService
    {
        Task SendNotificationToAll(string message);
        Task SendNotificationToUser(string userId, string message);
    }
}
