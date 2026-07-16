namespace NFC.Platform.Application.DTOs.Admin
{
    public class UpdateCardPricingDto
    {
        [Required]
        public CardType CardType { get; set; }

        [Required]
        [Range(0.001, 100000.000)]
        public decimal UnitPrice { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z]{3}$", ErrorMessage = "Currency must be exactly 3 alphabetic characters.")]
        public string Currency { get; set; } = "KWD";
    }
}
