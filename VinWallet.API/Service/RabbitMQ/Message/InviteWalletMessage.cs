using VinWallet.Repository.Enums;

namespace VinWallet.API.Service.RabbitMQ.Message
{
    public class InviteWalletMessage : BaseMessage
    {
        public override string NotificationType => MessageTypeEnum.InviteWallet.ToString();

        public Guid? WalletId { get; set; }
        public Guid? OwnerId { get; set; }
        public Guid? MemberId { get; set; }

    }
}
