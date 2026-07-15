using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class AdminOrderSummaryDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public CardType Material { get; set; }
        public CardDesignType DesignType { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
