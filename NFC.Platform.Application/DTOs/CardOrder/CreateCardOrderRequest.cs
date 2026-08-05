using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

public class CreateCardOrderRequest
{
    public Guid? CardDesignId { get; set; }

    public string? Notes { get; set; }

    public AssignmentScope? AssignmentScope { get; set; }

    public List<Guid>? EmployeeIds { get; set; }

    public int? QuantityPerEmployee { get; set; }

    public int? Quantity { get; set; }
}
