using FluentValidation;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardTemplate;

public class CreateCardTemplateValidator : AbstractValidator<CreateCardTemplateRequest>
{
    public CreateCardTemplateValidator(IMessageService messageService)
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

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("RequiredField", messageService.Get("CategoryId")));

        RuleFor(x => x.PhotoUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", messageService.Get("PhotoUrl")))
            .When(x => !string.IsNullOrWhiteSpace(x.PhotoUrl));

        RuleFor(x => x.FileUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", messageService.Get("FileUrl")))
            .When(x => !string.IsNullOrWhiteSpace(x.FileUrl));
    }
}
