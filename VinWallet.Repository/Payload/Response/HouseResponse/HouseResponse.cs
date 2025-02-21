using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.HouseResponse
{
    public class HouseResponse
    {
        public Guid Id { get; set; }

        public string? No { get; set; }

        public string? NumberOfRoom { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Code { get; set; }

        public int? BedroomCount { get; set; }

        public int? BathroomCount { get; set; }

        public bool? HasBalcony { get; set; }

        public string? FurnishingStatus { get; set; }

        public string? SquareMeters { get; set; }

        public string? Orientation { get; set; }

        public string? ContactTerms { get; set; }

        public string? Occupacy { get; set; }

        public Guid? BuildingId { get; set; }

        public Guid? HouseTypeId { get; set; }

        public HouseResponse(Guid id, string? no, string? numberOfRoom, string? status, DateTime? createdAt, DateTime? updatedAt, string? code, int? bedroomCount, int? bathroomCount, bool? hasBalcony, string? furnishingStatus, string? squareMeters, string? orientation, string? contactTerms, string? occupacy, Guid? buildingId, Guid? houseTypeId)
        {
            Id = id;
            No = no;
            NumberOfRoom = numberOfRoom;
            Status = status;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Code = code;
            BedroomCount = bedroomCount;
            BathroomCount = bathroomCount;
            HasBalcony = hasBalcony;
            FurnishingStatus = furnishingStatus;
            SquareMeters = squareMeters;
            Orientation = orientation;
            ContactTerms = contactTerms;
            Occupacy = occupacy;
            BuildingId = buildingId;
            HouseTypeId = houseTypeId;
        }
    }
}
