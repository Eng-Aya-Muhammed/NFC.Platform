using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Template;

    public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
    {
        public CreateTemplateRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.TemplateName)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("TemplateName")))
                .MinimumLength(3)
                .WithMessage(x => messageService.Get("MinLength", messageService.Get("TemplateName"), 3))
                .MaximumLength(150)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("TemplateName"), 150));

            RuleFor(x => x.LogoUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("LogoUrl"), 1000));

            RuleFor(x => x.ReferenceImageUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("ReferenceImageUrl"), 1000));

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Notes"), 1000));
        }
    }

