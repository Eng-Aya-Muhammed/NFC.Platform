using FluentValidation;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardType;

public class CreateCardTypeValidator : AbstractValidator<CreateCardTypeRequest>
{
    public CreateCardTypeValidator(IMessageService messageService)
    {
        RuleFor(x => x.NameAr)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("RequiredField", messageService.Get("NameAr")))
            .MaximumLength(200)
            .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("NameAr"), 200));

        RuleFor(x => x.NameEn)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("RequiredField", messageService.Get("NameEn")))
            .MaximumLength(200)
            .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("NameEn"), 200));

        RuleFor(x => x.PhotoUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", messageService.Get("PhotoUrl")))
            .When(x => !string.IsNullOrWhiteSpace(x.PhotoUrl));
    }
}
