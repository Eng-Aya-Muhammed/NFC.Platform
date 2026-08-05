using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

public class ReorderRequest
{
    public Guid? CardPackageId { get; set; }

    public AssignmentScope AssignmentScope { get; set; } = AssignmentScope.AllEmployees;

    public List<Guid> EmployeeIds { get; set; } = [];
}
