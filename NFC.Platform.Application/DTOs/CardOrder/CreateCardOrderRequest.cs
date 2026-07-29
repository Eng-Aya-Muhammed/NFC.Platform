using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Request payload for creating a new CardOrder against an existing (paid) CardDesign.
///
/// AccountType-specific rules (enforced in CardOrderService):
///   Company (CompanyAdmin):
///     - AssignmentScope required (AllEmployees | SpecificEmployees)
///     - EmployeeIds required when AssignmentScope = SpecificEmployees
///     - QuantityPerEmployee required (same count applied to every selected employee)
///     - Quantity is ignored
///   Individual:
///     - Quantity required
///     - AssignmentScope, EmployeeIds, QuantityPerEmployee are ignored
/// </summary>
public class CreateCardOrderRequest
{
    // ── Shared ───────────────────────────────────────────────────────────
    /// <summary>
    /// The paid CardDesign this order is based on.
    /// Must exist, belong to the current tenant, and have IsPaid = true.
    /// If null/omitted, auto-resolves the latest paid CardDesign with available capacity.
    /// </summary>
    public Guid? CardDesignId { get; set; }

    public string? Notes { get; set; }

    // ── Company-only (ignored for Individual) ────────────────────────────
    /// <summary>AllEmployees or SpecificEmployees (ExcelUpload is not valid here).</summary>
    public AssignmentScope? AssignmentScope { get; set; }

    /// <summary>Required when AssignmentScope = SpecificEmployees.</summary>
    public List<Guid>? EmployeeIds { get; set; }

    /// <summary>Fixed number of physical cards requested for each selected employee.</summary>
    public int? QuantityPerEmployee { get; set; }

    // ── Individual-only (ignored for Company) ────────────────────────────
    /// <summary>Total number of physical cards for the individual user.</summary>
    public int? Quantity { get; set; }
}
