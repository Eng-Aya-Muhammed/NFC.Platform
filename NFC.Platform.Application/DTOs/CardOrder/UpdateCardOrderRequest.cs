using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

public class UpdateCardOrderRequest
{
    public AssignmentScope? AssignmentScope { get; set; }
    public List<Guid>? EmployeeIds { get; set; }
    public int? QuantityPerEmployee { get; set; }

    public int? Quantity { get; set; }

    public string? Notes { get; set; }
}
