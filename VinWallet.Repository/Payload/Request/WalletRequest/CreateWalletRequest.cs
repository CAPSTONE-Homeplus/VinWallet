using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Request.WalletRequest
{
    public class CreateWalletRequest
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public Guid? OwnerId { get; set; }
    }
}
