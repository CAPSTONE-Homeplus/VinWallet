using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.RoomResponse
{
    public class RoomResponse
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Status { get; set; }

        public decimal? Size { get; set; }

        public bool? FurnitureIncluded { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? SquareMeters { get; set; }

        public Guid? HouseId { get; set; }

        public Guid? RoomTypeId { get; set; }

        public string? Code { get; set; }
    }
}
