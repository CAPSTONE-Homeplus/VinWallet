using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Utils
{
    public class PasswordUtil
    {
        public static string HashPassword(string rawPassword)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawPassword));
                string ahihi = Convert.ToBase64String(bytes);
                return ahihi;
            }
        }
    }
}
