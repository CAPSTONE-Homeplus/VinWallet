namespace VinWallet.API.Service.RabbitMQ
{
    public class RabbitMQOptions
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Exchange { get; set; }
        public Dictionary<string, string> Queues { get; set; }
    }
}
