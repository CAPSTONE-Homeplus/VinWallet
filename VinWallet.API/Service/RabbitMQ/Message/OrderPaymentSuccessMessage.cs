namespace VinWallet.API.Service.RabbitMQ.Message
{
    public class OrderPaymentSuccessMessage
    {
        public Guid TransactionId { get; set; }
        public Guid? WalletId { get; set; }
        public string Amount { get; set; }
        public DateTime? Timestamp { get; set; }
        public Guid? OrderId { get; set; }
    }
}
