namespace NFC.Platform.Application.DTOs.Admin
{
    public class UpdateCardPricingDto
    {
        public CardType CardType { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "KWD";
    }
}
