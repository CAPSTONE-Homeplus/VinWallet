namespace VinWallet.API.Service.Interfaces
{
    public interface ISignalRHubService
    {
        Task SendNotificationToAll(object message);
        Task SendNotificationToUser(string userId, object message);
        Task SendNotificationToGroup(string groupName, object message);
    }
}
