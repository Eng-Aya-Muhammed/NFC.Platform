namespace NFC.Platform.Domain.Entities
{
    public class CardPricing : BaseEntity
    {
        public CardType CardType { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "KWD";
        public bool IsActive { get; set; } = true;
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; } // null = currently active
    }
}
