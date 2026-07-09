using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators
{
    public class UpdateCompanyProfileRequestValidator : AbstractValidator<UpdateCompanyProfileRequest>
    {
        public UpdateCompanyProfileRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Name"))
                .MaximumLength(200)
                .WithMessage(x => messageService.Get("MaxLength", "Name", 200));

            RuleFor(x => x.Activity)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Activity"))
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", "Activity", 256));

            RuleFor(x => x.CommercialRegistry)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "CommercialRegistry"))
                .MaximumLength(100)
                .WithMessage(x => messageService.Get("MaxLength", "CommercialRegistry", 100));

            RuleFor(x => x.Size)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Size"))
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", "Size", 50));

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Address"))
                .MaximumLength(500)
                .WithMessage(x => messageService.Get("MaxLength", "Address", 500));
        }
    }
}
