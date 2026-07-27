using FluentValidation;
using NFC.Platform.Application.DTOs.CardPackage;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardPackage;

public class UpdateCardPackageValidator : AbstractValidator<UpdateCardPackageRequest>
{
    public UpdateCardPackageValidator(IMessageService messageService)
    {
        RuleFor(x => x.NumberOfCards)
            .GreaterThan(0)
            .WithMessage(_ => messageService.Get("InvalidValue", messageService.Get("NumberOfCards")))
            .When(x => x.NumberOfCards.HasValue);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage(_ => messageService.Get("InvalidValue", messageService.Get("Price")))
            .When(x => x.Price.HasValue);
    }
}
