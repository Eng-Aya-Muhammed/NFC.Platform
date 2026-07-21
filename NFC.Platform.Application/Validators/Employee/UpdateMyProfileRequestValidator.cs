using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Employee;

    public class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
    {
        public UpdateMyProfileRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", messageService.Get("FullName")))
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("FullName"), 256));

            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Bio"), 500));

            RuleFor(x => x.ProfilePictureUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("ProfilePictureUrl"), 1000));

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.ContactEmail))
                .WithMessage(x => messageService.Get("InvalidEmail", messageService.Get("ContactEmail")))
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("ContactEmail"), 256));

            RuleFor(x => x.Phone)
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("Phone"), 50));

            RuleFor(x => x.WhatsApp)
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", messageService.Get("WhatsApp"), 50));

        }
    }

