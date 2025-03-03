namespace VinWallet.API.Service.RabbitMQ.Message
{
    public class InviteWalletMessage
    {
        public Guid? OwnerId { get; set; }
        public Guid? MemberId { get; set; }
        public Guid WalletId { get; set; }
    }
}
