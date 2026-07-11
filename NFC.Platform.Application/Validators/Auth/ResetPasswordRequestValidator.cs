using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Auth;

    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Token"));

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "NewPassword"))
                .MinimumLength(6)
                .WithMessage(x => messageService.Get("MinLength", "NewPassword", 6));

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "ConfirmNewPassword"))
                .Equal(x => x.NewPassword)
                .WithMessage(x => messageService.Get("PasswordMismatch"));
        }
    }

