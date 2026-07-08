using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Email"))
                .EmailAddress()
                .WithMessage(x => messageService.Get("InvalidEmail", "Email"));

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Password"));
        }
    }
}
