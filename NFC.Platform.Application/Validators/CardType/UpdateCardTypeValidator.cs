using FluentValidation;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardType;

public class UpdateCardTypeValidator : AbstractValidator<UpdateCardTypeRequest>
{
    public UpdateCardTypeValidator(IMessageService messageService)
    {
        RuleFor(x => x.NameAr)
            .MaximumLength(200)
            .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("NameAr"), 200))
            .When(x => !string.IsNullOrWhiteSpace(x.NameAr));

        RuleFor(x => x.NameEn)
            .MaximumLength(200)
            .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("NameEn"), 200))
            .When(x => !string.IsNullOrWhiteSpace(x.NameEn));

        RuleFor(x => x.PhotoUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", messageService.Get("PhotoUrl")))
            .When(x => !string.IsNullOrWhiteSpace(x.PhotoUrl));
    }
}
