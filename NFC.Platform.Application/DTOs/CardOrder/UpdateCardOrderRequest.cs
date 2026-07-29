using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Request payload for updating an existing CardOrder (PendingReview only).
/// Design and payment fields are now owned by CardDesign and cannot be changed here.
/// </summary>
public class UpdateCardOrderRequest
{
    // ── Employee Assignment (Company-only) ───────────────────────────────
    /// <summary>AllEmployees or SpecificEmployees (ExcelUpload is not valid here).</summary>
    public AssignmentScope? AssignmentScope { get; set; }
    public List<Guid>? EmployeeIds { get; set; }
    public int? QuantityPerEmployee { get; set; }

    // ── Individual-only ───────────────────────────────────────────────────
    public int? Quantity { get; set; }

    // ── Delivery Notes ───────────────────────────────────────────────────
    public string? Notes { get; set; }
}
