namespace NFC.Platform.Application.DTOs.DiscountCode;

public class ValidateDiscountCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
}
