namespace VinWallet.API.Service.RabbitMQ.Message
{
    public abstract class BaseMessage
    {
        public abstract string NotificationType { get; }
    }
}
