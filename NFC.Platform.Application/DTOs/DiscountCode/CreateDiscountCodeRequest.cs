using System;

namespace NFC.Platform.Application.DTOs.DiscountCode;

public class CreateDiscountCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
