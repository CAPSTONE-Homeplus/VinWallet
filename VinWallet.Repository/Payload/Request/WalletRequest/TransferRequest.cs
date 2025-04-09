using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Request.WalletRequest
{
    public class TransferRequest
    {
        public Guid SharedWalletId { get; set; }
        public Guid PersonalWalletId { get; set; }
        public decimal Amount { get; set; }
    }
}
