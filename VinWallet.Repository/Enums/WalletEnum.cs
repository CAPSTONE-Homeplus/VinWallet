using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Enums
{
    public class WalletEnum
    {
        public enum WalletType
        {
            Shared = 1,
            Personal = 2
        }
        public enum WalletStatus
        {
            Active = 1,
            Inactive = 2,
            Dissolved = 3
        }

        public enum UserWalletStatus
        {
            Joined = 1,
            Left = 2
        }
    }
}
