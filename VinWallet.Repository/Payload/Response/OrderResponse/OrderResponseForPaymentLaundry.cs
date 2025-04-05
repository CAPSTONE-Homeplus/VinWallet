using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.OrderResponse
{
    public class OrderResponseForPaymentLaundry
    {
        public Guid Id { get; set; }

        public string? OrderCode { get; set; }

        public string? Name { get; set; }

        public Guid? UserId { get; set; }

        public decimal? Balance { get; set; }

        public string? Currency { get; set; }

        public string? Type { get; set; }

        public string? ExtraField { get; set; }

        public decimal? TotalAmount { get; set; }

        public decimal? DiscountAmount { get; set; }

        public DateTime? OrderDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? EstimatedCompletionTime { get; set; }

        public Guid? AppliedDiscountId { get; set; }
    }
}
