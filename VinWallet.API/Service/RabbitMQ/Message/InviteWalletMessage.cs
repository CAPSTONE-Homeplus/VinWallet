using VinWallet.Repository.Enums;

namespace VinWallet.API.Service.RabbitMQ.Message
{
    public class InviteWalletMessage
    {
        public Guid? WalletId { get; set; }
        public Guid? OwnerId { get; set; }
        public Guid? MemberId { get; set; }

    }
}
