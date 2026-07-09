using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators
{
    public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
    {
        public UpdateEmployeeRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.JobTitle)
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", "JobTitle", 256));

            RuleFor(x => x.Department)
                .MaximumLength(150)
                .WithMessage(x => messageService.Get("MaxLength", "Department", 150));

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage(x => messageService.Get("InvalidValue", "Status"));
        }
    }
}
