using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class UpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }

        public string? TrackingNumber { get; set; }

        public string? RejectionReason { get; set; }
    }
}
