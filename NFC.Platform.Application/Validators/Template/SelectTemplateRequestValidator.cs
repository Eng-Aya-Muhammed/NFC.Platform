using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Template;

    public class SelectTemplateRequestValidator : AbstractValidator<SelectTemplateRequest>
    {
        public SelectTemplateRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("TemplateId")));
        }
    }

