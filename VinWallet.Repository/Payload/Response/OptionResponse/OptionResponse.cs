using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.OptionResponse
{
    public class OptionResponse
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public decimal? Price { get; set; }

        public string? Note { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool? IsMandatory { get; set; }

        public int? MaxQuantity { get; set; }

        public int? Discount { get; set; }

        public string? Code { get; set; }

        public Guid? ServiceId { get; set; }
    }
}
