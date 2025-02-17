using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.TransactionResponse
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }

        public Guid? WalletId { get; set; }

        public Guid? UserId { get; set; }

        public Guid? PaymentMethodId { get; set; }

        public string? Amount { get; set; }

        public string? Type { get; set; }

        public string? Note { get; set; }

        public DateTime? TransactionDate { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Code { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid? OrderId { get; set; }
    }
}
