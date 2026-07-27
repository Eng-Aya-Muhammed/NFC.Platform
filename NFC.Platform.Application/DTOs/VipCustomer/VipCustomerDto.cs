using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.VipCustomer;

public class VipCustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public VipCustomerType CustomerType { get; set; }
    public int VipDisplayOrder { get; set; }
    public bool IsVip { get; set; }
}
