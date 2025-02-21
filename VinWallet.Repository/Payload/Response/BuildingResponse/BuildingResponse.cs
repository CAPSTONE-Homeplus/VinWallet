using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Response.BuildingResponse
{
    public class BuildingResponse
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Longitude { get; set; }

        public string? Latitude { get; set; }

        public string? Code { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? HubId { get; set; }

        public Guid? ClusterId { get; set; }

        public string? Status { get; set; }

        public BuildingResponse()
        {
        }
        public BuildingResponse(Guid guid, string? name, string? longitude, string? latitude, string? code, DateTime? createdAt, DateTime? updatedAt, Guid? hubId, Guid? clusterId, string? status)
        {
            Id = guid;
            Name = name;
            Longitude = longitude;
            Latitude = latitude;
            Code = code;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            HubId = hubId;
            ClusterId = clusterId;
            Status = status;
        }
    }
}
