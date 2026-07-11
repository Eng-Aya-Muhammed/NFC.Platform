using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.Auth;

    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator(IMessageService messageService)
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

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "ConfirmPassword"))
                .Equal(x => x.Password)
                .WithMessage(x => messageService.Get("PasswordMismatch"));

            RuleFor(x => x.AccountType)
                .IsInEnum()
                .WithMessage(x => messageService.Get("InvalidValue", "AccountType"));

            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .When(x => x.AccountType == AccountType.CompanyAdmin)
                .WithMessage(x => messageService.Get("RequiredField", "CompanyName"))
                .MaximumLength(200)
                .WithMessage(x => messageService.Get("MaxLength", "CompanyName", 200));
        }
    }


