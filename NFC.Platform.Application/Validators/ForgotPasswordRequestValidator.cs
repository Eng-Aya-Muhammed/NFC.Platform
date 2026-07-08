using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators
{
    public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
    {
        public ForgotPasswordRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Email"))
                .EmailAddress()
                .WithMessage(x => messageService.Get("InvalidEmail", "Email"));
        }
    }
}
