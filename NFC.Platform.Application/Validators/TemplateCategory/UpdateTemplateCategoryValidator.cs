using FluentValidation;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.TemplateCategory;

public class UpdateTemplateCategoryValidator : AbstractValidator<UpdateTemplateCategoryRequest>
{
    public UpdateTemplateCategoryValidator(IMessageService messageService)
    {
        RuleFor(x => x.NameAr)
            .MaximumLength(200)
            .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("NameAr"), 200))
            .When(x => !string.IsNullOrWhiteSpace(x.NameAr));

        RuleFor(x => x.NameEn)
            .MaximumLength(200)
            .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("NameEn"), 200))
            .When(x => !string.IsNullOrWhiteSpace(x.NameEn));
    }
}
