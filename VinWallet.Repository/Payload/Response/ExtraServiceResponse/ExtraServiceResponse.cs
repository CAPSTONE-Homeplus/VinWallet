using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.ExtraServiceResponse
{
    public class ExtraServiceResponse
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public decimal? Price { get; set; }

        public string? Status { get; set; }

        public int? ExtraTime { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Code { get; set; }

        public Guid? ServiceId { get; set; }
    }
}
