namespace NFC.Platform.Application.DTOs.DiscountCode;

public class DiscountCodeValidationResultDto
{
    public bool IsValid { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal CalculatedDiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? ErrorMessage { get; set; }
}
