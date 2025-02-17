using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace VinWallet.Repository.Payload.Response.OrderResponse
{
    public class OrderResponse
    {
        public Guid Id { get; set; }

        public string? Note { get; set; }

        public decimal? Price { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Address { get; set; }

        public DateTime? BookingDate { get; set; }

        public Guid? EmployeeId { get; set; }

        public double? EmployeeRating { get; set; }

        public string? CustomerFeedback { get; set; }

        public bool? CleaningToolsRequired { get; set; }

        public bool? CleaningToolsProvided { get; set; }

        public string? ServiceType { get; set; }

        public double? DistanceToCustomer { get; set; }

        public int? PriorityLevel { get; set; }

        public string? Notes { get; set; }

        public string? DiscountCode { get; set; }

        public string? DiscountAmount { get; set; }

        public decimal? TotalAmount { get; set; }

        public string? RealTimeStatus { get; set; }

        public DateTime? JobStartTime { get; set; }

        public DateTime? JobEndTime { get; set; }

        public bool? EmergencyRequest { get; set; }

        public string? CleaningAreas { get; set; }

        public string? ItemsToClean { get; set; }

        public DateTime? EstimatedArrivalTime { get; set; }

        public int? EstimatedDuration { get; set; }

        public int? ActualDuration { get; set; }

        public DateTime? CancellationDeadline { get; set; }

        public string? Code { get; set; }

        public Guid? TimeSlotId { get; set; }

        public Guid? ServiceId { get; set; }

        public Guid? UserId { get; set; }

        public List<ExtraServiceResponse.ExtraServiceResponse>? ExtraServices { get; set; }

        public List<OptionResponse.OptionResponse>? Options { get; set; }

    }
}
