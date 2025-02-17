using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Request.TransactionRequest
{
    public class CreateTransactionRequest
    {
        public Guid? WalletId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public Guid? UserId { get; set; }

        [Required(ErrorMessage = "PaymentMethodId is required")]
        public Guid? PaymentMethodId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        public string? Amount { get; set; }

        public string? Note { get; set; }

        public Guid? OrderId { get; set; }
    }
}
