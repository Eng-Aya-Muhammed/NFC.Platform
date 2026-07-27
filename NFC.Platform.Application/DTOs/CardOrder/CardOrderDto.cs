using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Data transfer object representing a CardOrder response.
/// </summary>
public class CardOrderDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public Guid CardTypeId { get; set; }
    public Guid CardPackageId { get; set; }
    public CardDesignType CardDesignType { get; set; }
    public Guid? ParentOrderId { get; set; }
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
    public string? TrackingNumber { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CardOrderItemDto> Items { get; set; } = [];
}
