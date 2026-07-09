using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators
{
    public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
    {
        public CreateEmployeeRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Username"))
                .MaximumLength(150)
                .WithMessage(x => messageService.Get("MaxLength", "Username", 150));

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Email"))
                .EmailAddress()
                .WithMessage(x => messageService.Get("InvalidEmail", "Email"));

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "FullName"))
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", "FullName", 256));

            RuleFor(x => x.JobTitle)
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", "JobTitle", 256));

            RuleFor(x => x.Department)
                .MaximumLength(150)
                .WithMessage(x => messageService.Get("MaxLength", "Department", 150));
        }
    }
}
