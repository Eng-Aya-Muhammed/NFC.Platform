using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators
{
    public class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
    {
        public UpdateMyProfileRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "FullName"))
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", "FullName", 256));

            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .WithMessage(x => messageService.Get("MaxLength", "Bio", 500));

            RuleFor(x => x.ProfilePictureUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", "ProfilePictureUrl", 1000));

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.ContactEmail))
                .WithMessage(x => messageService.Get("InvalidEmail", "ContactEmail"))
                .MaximumLength(256)
                .WithMessage(x => messageService.Get("MaxLength", "ContactEmail", 256));

            RuleFor(x => x.Phone)
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", "Phone", 50));

            RuleFor(x => x.WhatsApp)
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", "WhatsApp", 50));

            RuleFor(x => x.InstagramUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", "InstagramUrl", 1000));

            RuleFor(x => x.FacebookUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", "FacebookUrl", 1000));

            RuleFor(x => x.LinkedInUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", "LinkedInUrl", 1000));

            RuleFor(x => x.WebsiteUrl)
                .MaximumLength(1000)
                .WithMessage(x => messageService.Get("MaxLength", "WebsiteUrl", 1000));
        }
    }
}
