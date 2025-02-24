using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.TransactionResponse
{
    public class GetTransactionResponse
    {
        public Guid Id { get; set; }

        public Guid? WalletId { get; set; }

        public Guid? UserId { get; set; }

        public Guid? PaymentMethodId { get; set; }

        public string? Amount { get; set; }

        public string? Type { get; set; }

        public string? PaymentUrl { get; set; }

        public string? Note { get; set; }

        public DateTime? TransactionDate { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Code { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid? OrderId { get; set; }

        public GetTransactionResponse(Guid guid, Guid? walletId, Guid? userId, Guid? paymentMethodId, string? amount, string? type, string? paymentUrl, string? note, DateTime? transactionDate, string? status, DateTime? createdAt, DateTime? updatedAt, string? code, Guid? categoryId, Guid? orderId)
        {
            Id = guid;
            WalletId = walletId;
            UserId = userId;
            PaymentMethodId = paymentMethodId;
            Amount = amount;
            Type = type;
            PaymentUrl = paymentUrl;
            Note = note;
            TransactionDate = transactionDate;
            Status = status;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Code = code;
            CategoryId = categoryId;
            OrderId = orderId;
        }
    }
}
