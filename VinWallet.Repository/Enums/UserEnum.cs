using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Enums
{
    public class UserEnum
    {
        public enum Status
        {
            Active = 1,
            Inactive = 2
        }

        public enum Role
        {
            Leader = 1,
            Member = 2
        }
    }
}
