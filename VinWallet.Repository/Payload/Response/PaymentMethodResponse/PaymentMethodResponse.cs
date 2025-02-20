using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.PaymentMethodResponse
{
    public class PaymentMethodResponse
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Status { get; set; }
        public PaymentMethodResponse() { }
        public PaymentMethodResponse(Guid id, string? name, string? description, DateTime? createdAt, DateTime? updatedAt, string? status)
        {
            Id = id;
            Name = name;
            Description = description;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Status = status;
        }
    }
}
