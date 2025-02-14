using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.UserResponse
{
    public class UserResponse
    {
        public Guid Id { get; set; }

        public string? FullName { get; set; }

        public string? Status { get; set; }

        public Guid? RoomId { get; set; }
    }
}
