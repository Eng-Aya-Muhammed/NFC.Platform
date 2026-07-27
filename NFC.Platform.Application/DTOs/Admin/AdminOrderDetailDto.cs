using System;
using System.Collections.Generic;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class AdminOrderDetailDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public Guid CardTypeId { get; set; }
        public Guid CardPackageId { get; set; }
        public CardDesignType DesignType { get; set; }
        public int Quantity { get; set; }
        public string? ExcelDataUrl { get; set; }
        public string? FrontDesignUrl { get; set; }
        public string? BackDesignUrl { get; set; }
        public string? Notes { get; set; }
        public OrderStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "KWD";
        public DeliveryMethod DeliveryMethod { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShippingAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CardOrderItemDto> Items { get; set; } = [];
    }
}
