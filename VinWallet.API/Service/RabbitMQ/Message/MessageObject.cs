namespace VinWallet.API.Service.RabbitMQ.Message
{
    public class MessageObject<T> where T : BaseMessage
    {
        public T Data { get; set; }
    }
}
