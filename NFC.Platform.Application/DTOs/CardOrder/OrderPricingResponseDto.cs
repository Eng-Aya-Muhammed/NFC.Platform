namespace NFC.Platform.Application.DTOs.CardOrder;

public class OrderPricingResponseDto
{
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "KWD";
}
