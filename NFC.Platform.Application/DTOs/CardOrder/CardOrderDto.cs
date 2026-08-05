using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

public class CardOrderDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    public Guid? CardDesignId { get; set; }

    public string CardName { get; set; } = string.Empty;

    public Guid? ParentOrderId { get; set; }
    public int Quantity { get; set; }

    public int QuantityPerEmployee { get; set; }

    public string? Notes { get; set; }
    public OrderStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "KWD";
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CardOrderItemDto> Items { get; set; } = [];
}
