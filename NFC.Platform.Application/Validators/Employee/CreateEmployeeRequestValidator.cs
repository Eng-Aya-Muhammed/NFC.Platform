using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Employee;

    public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
    {
        public CreateEmployeeRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("Email")))
                .EmailAddress()
                .WithMessage(x => messageService.Get("InvalidEmail", messageService.Get("Email")));

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("FullName")))
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("FullName"), 256));

            RuleFor(x => x.JobTitle)
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("JobTitle"), 256));

            RuleFor(x => x.Department)
                .MaximumLength(150)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Department"), 150));

            RuleFor(x => x.ProfilePictureUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", "ProfilePictureUrl", 1000))
                .MustBeValidUrl()
                .WithMessage(x => messageService.Get("InvalidUrlFormat", "Profile Picture URL"));

            RuleFor(x => x.Phone)
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", "Phone", 50))
                .MustBeValidPhoneNumber()
                .WithMessage(_ => messageService.Get("InvalidPhoneFormat"));

            RuleFor(x => x.WhatsApp)
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", "WhatsApp", 50))
                .MustBeValidPhoneNumber()
                .WithMessage(_ => messageService.Get("InvalidPhoneFormat"));

        }
    }


