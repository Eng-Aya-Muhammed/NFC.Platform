using System;

namespace NFC.Platform.Application.DTOs.CardOrder;

    /// <summary>
    /// Data transfer object representing a single CardOrderItem.
    /// </summary>
    public class CardOrderItemDto
    {
        public Guid Id { get; set; }
        public Guid CardOrderId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public Guid? LinkedCardId { get; set; }
        public Guid? UserProfileId { get; set; }
    }

