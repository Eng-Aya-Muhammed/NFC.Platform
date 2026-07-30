using FluentValidation;
using NFC.Platform.Application.DTOs.Auth;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Auth
{
    public class ResendOtpRequestValidator : AbstractValidator<ResendOtpRequest>
    {
        public ResendOtpRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Email"))
                .EmailAddress()
                .WithMessage(x => messageService.Get("InvalidEmail", "Email"));
        }
    }
}
