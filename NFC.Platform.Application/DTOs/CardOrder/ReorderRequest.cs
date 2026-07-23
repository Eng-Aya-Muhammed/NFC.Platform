using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Request body for the "إضافة طلب" (reorder) modal.
/// Creates a new order reusing the parent order's design/template.
/// </summary>
public class ReorderRequest
{
    public int Quantity { get; set; }

    /// <summary>
    /// Assignment scope (AllEmployees, SpecificEmployees, or Individual).
    /// </summary>
    public AssignmentScope AssignmentScope { get; set; } = AssignmentScope.AllEmployees;

    /// <summary>
    /// Required when AssignmentScope = AssignmentScope.SpecificEmployees.
    /// Count must equal Quantity.
    /// </summary>
    public List<Guid> EmployeeIds { get; set; } = [];

    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Pickup;

    public string? ShippingAddress { get; set; }
}
