namespace VinWallet.API.Hubs.Message
{
    public class MessageObject<T> where T : class
    {
        public string Type { get; set; }
        public T Data { get; set; }
    }
}
