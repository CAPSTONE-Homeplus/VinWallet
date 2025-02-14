using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public String Status { get; set; }
        public string Role { get; set; }
    }
}
