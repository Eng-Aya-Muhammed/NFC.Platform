namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Response from the price calculator endpoint (GET /api/order-draft/pricing).
/// The server owns the pricing logic; clients must never send a total.
/// </summary>
public class OrderPricingResponseDto
{
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "KWD";
}
