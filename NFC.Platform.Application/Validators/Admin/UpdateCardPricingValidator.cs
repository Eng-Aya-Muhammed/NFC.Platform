namespace NFC.Platform.Application.Validators.Admin
{
    public class UpdateCardPricingValidator : AbstractValidator<UpdateCardPricingDto>
    {
        public UpdateCardPricingValidator(IMessageService messageService)
        {
            RuleFor(x => x.CardType)
                .IsInEnum()
                .WithMessage(x => messageService.Get("InvalidValue", "CardType"));

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0)
                .WithMessage(x => messageService.Get("UnitPriceMustBeGreaterThanZero") ?? "UnitPrice must be greater than zero.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Currency"))
                .Must(c => !string.IsNullOrEmpty(c) && Regex.IsMatch(c.Trim(), @"^[a-zA-Z]{3}$"))
                .WithMessage(x => messageService.Get("CurrencyMustBeThreeLetters") ?? "Currency must be exactly 3 alphabetic characters.");
        }
    }
}
