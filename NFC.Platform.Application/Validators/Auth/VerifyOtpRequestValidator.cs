using FluentValidation;
using NFC.Platform.Application.DTOs.Auth;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Auth
{
    public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
    {
        public VerifyOtpRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage(x => messageService.Get("InvalidEmail", "Email"));

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "OtpCode"))
                .Length(6)
                .WithMessage(x => messageService.Get("MinLength", "OtpCode", 6));
        }
    }
}
