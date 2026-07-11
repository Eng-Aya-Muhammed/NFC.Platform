using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Company;

    public class UpdateCompanyProfileRequestValidator : AbstractValidator<UpdateCompanyProfileRequest>
    {
        public UpdateCompanyProfileRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("Name")))
                .MaximumLength(200)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Name"), 200));

            RuleFor(x => x.Activity)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("Activity")))
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Activity"), 256));

            RuleFor(x => x.CommercialRegistry)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("CommercialRegistry")))
                .MaximumLength(100)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("CommercialRegistry"), 100));

            RuleFor(x => x.Size)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("Size")))
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Size"), 50));

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("Address")))
                .MaximumLength(500)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Address"), 500));

            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("Phone")))
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Phone"), 50));
        }
    }

