using FluentValidation;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.TemplateCategory;

public class CreateTemplateCategoryValidator : AbstractValidator<CreateTemplateCategoryRequest>
{
    public CreateTemplateCategoryValidator(IMessageService messageService)
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
    }
}
