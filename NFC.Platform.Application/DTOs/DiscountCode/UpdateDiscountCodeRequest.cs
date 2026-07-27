using System;

namespace NFC.Platform.Application.DTOs.DiscountCode;

public class UpdateDiscountCodeRequest
{
    public string? Code { get; set; }
    public decimal? DiscountValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
