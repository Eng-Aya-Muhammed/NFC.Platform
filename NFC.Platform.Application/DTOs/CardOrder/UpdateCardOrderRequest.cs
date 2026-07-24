using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Request payload for updating an existing CardOrder.
/// Allows updating general order information or modifying items via new Excel or explicit EmployeeIds.
/// </summary>
public class UpdateCardOrderRequest
{
    // Design & Structure
    public CardDesignType? CardDesignType { get; set; }
    public AssignmentScope? AssignmentScope { get; set; }
    
    public string? CardName { get; set; }

    // Employee Assignment Data
    public List<Guid>? EmployeeIds { get; set; }
    public string? ExcelDataUrl { get; set; }

    // Design Files
    public string? FrontDesignUrl { get; set; }
    public string? BackDesignUrl { get; set; }

    // Pricing factors
    public CardType? CardType { get; set; }
    public int? Quantity { get; set; }

    // General
    public string? Notes { get; set; }
    public DeliveryMethod? DeliveryMethod { get; set; }
    public string? ShippingAddress { get; set; }
}
