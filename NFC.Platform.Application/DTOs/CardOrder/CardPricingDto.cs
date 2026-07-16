namespace NFC.Platform.Application.DTOs.CardOrder
{
    public class CardPricingDto
    {
        public CardType CardType { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "KWD";
    }
}
