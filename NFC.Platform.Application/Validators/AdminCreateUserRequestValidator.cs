using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators
{
    public class AdminCreateUserRequestValidator : AbstractValidator<AdminCreateUserRequest>
    {
        public AdminCreateUserRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Username"));

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Email"))
                .EmailAddress()
                .WithMessage(x => messageService.Get("InvalidEmail", "Email"));

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Password"))
                .MinimumLength(6)
                .WithMessage(x => messageService.Get("MinLength", "Password", 6));

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage(x => messageService.Get("InvalidRole"));
        }
    }
}
