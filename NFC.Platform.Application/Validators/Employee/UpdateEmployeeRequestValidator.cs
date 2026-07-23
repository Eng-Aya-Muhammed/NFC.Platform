using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Employee;

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator(IMessageService messageService)
    {
        RuleFor(x => x.FullName)
            .MaximumLength(256)
            .WithMessage(x => messageService.Get("MaxLength", messageService.Get("FullName"), 256));

        RuleFor(x => x.JobTitle)
            .MaximumLength(256)
            .WithMessage(x => messageService.Get("MaxLength", messageService.Get("JobTitle"), 256));

        RuleFor(x => x.Department)
            .MaximumLength(150)
            .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Department"), 150));

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage(x => messageService.Get("InvalidValue", messageService.Get("Status")));

        RuleFor(x => x.Bio)
            .MaximumLength(1000)
            .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Bio"), 1000));

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Phone"), 50)).MustBeValidPhoneNumber().WithMessage(_ => messageService.Get("InvalidPhoneFormat"));

        RuleFor(x => x.WhatsApp)
            .MaximumLength(50)
            .WithMessage(x => messageService.Get("MaxLength", messageService.Get("WhatsApp"), 50)).MustBeValidPhoneNumber().WithMessage(_ => messageService.Get("InvalidPhoneFormat"));

        RuleForEach(x => x.Links).ChildRules(link => {
            link.RuleFor(l => l.Url)
                .MustBeValidUrl()
                .WithMessage(x => messageService.Get("InvalidUrlFormat", "URL"));
        });
    }

}



