using FluentValidation;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardTemplate;

public class UpdateCardTemplateValidator : AbstractValidator<UpdateCardTemplateRequest>
{
    public UpdateCardTemplateValidator(IMessageService messageService)
    {
        RuleFor(x => x.NameAr)
            .MaximumLength(200)
            .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("NameAr"), 200))
            .When(x => !string.IsNullOrWhiteSpace(x.NameAr));

        RuleFor(x => x.NameEn)
            .MaximumLength(200)
            .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("NameEn"), 200))
            .When(x => !string.IsNullOrWhiteSpace(x.NameEn));

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("RequiredField", messageService.Get("CategoryId")))
            .When(x => x.CategoryId.HasValue);

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
