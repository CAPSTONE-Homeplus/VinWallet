using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Request.WalletRequest
{
    public class ConnectWalletToUserRequest
    {
        public Guid UserId { get; set; }
        public Guid WalletId { get; set; }
    }
}
