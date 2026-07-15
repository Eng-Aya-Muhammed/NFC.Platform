using System.ComponentModel.DataAnnotations;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus Status { get; set; }

        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Mandatory when transitioning to Rejected.
        /// </summary>
        public string? RejectionReason { get; set; }
    }
}
