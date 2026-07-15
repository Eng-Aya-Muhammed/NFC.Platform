using FluentValidation;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Admin
{
    public class UpdateCardTemplateValidator : AbstractValidator<UpdateCardTemplateDto>
    {
        public UpdateCardTemplateValidator(IMessageService messageService)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "TemplateName"))
                .MaximumLength(200)
                .WithMessage(x => messageService.Get("MaxLength", "TemplateName", "200"));

            RuleFor(x => x.Category)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Category"))
                .MaximumLength(100)
                .WithMessage(x => messageService.Get("MaxLength", "Category", "100"));

            RuleFor(x => x.StyleConfigJson)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "StyleConfig"));
        }
    }
}
