using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Data transfer object representing a CardOrder response.
/// Design & CardType details are accessed via CardDesignId on the related CardDesign.
/// </summary>
public class CardOrderDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Reference to the paid CardDesign this order was created from.</summary>
    public Guid? CardDesignId { get; set; }

    /// <summary>Localized card type name based on current UI culture.</summary>
    public string CardName { get; set; } = string.Empty;

    public Guid? ParentOrderId { get; set; }
    public int Quantity { get; set; }

    /// <summary>Cards per employee (Company orders). 1 for Individual orders.</summary>
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
