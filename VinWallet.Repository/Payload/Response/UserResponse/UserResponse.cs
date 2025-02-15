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

        public string? ExtraField { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Username { get; set; }

        public string? Role { get; set; }
    }
}
