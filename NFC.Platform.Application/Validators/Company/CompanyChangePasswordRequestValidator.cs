using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Company;

    public class CompanyChangePasswordRequestValidator : AbstractValidator<CompanyChangePasswordRequest>
    {
        public CompanyChangePasswordRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "OldPassword"));

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

