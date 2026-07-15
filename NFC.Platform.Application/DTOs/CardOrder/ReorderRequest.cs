using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Request body for the "إضافة طلب" (reorder) modal.
/// Creates a new order reusing the parent order's design/template.
/// </summary>
public class ReorderRequest
{
    [Required]
    [Range(1, 10000)]
    public int Quantity { get; set; }

    /// <summary>
    /// "all_employees" assigns cards across all current employees.
    /// "specific_employees" requires EmployeeIds to be provided.
    /// </summary>
    [Required]
    public string AssignmentScope { get; set; } = "all_employees";

    /// <summary>
    /// Required when AssignmentScope = "specific_employees".
    /// Count must equal Quantity.
    /// </summary>
    public List<Guid> EmployeeIds { get; set; } = [];
}
