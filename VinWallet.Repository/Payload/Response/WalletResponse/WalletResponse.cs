using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.WalletResponse
{
    public class WalletResponse
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public decimal? Balance { get; set; }

        public string? Currency { get; set; }

        public string? Type { get; set; }

        public string? ExtraField { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? Status { get; set; }

        public Guid? OwnerId { get; set; }
    }   
}
