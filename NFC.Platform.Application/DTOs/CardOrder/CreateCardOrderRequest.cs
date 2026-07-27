using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Request payload for creating a new CardOrder.
/// </summary>
public class CreateCardOrderRequest
{
    public CardDesignType CardDesignType { get; set; }
    public AssignmentScope AssignmentScope { get; set; }
    public List<Guid>? EmployeeIds { get; set; }

    public string? CardName { get; set; }

    public string? ExcelDataUrl { get; set; }
    public string? FrontDesignUrl { get; set; }
    public string? BackDesignUrl { get; set; }

    public Guid CardTypeId { get; set; }
    public Guid CardPackageId { get; set; }

    public int Quantity { get; set; }

    public string? Notes { get; set; }

    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Pickup;
    public string? ShippingAddress { get; set; }
}
