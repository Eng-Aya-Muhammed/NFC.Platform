using FluentValidation;
using NFC.Platform.Application.DTOs.Auth;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.Auth
{
    public class GoogleRegisterRequestValidator : AbstractValidator<GoogleRegisterRequest>
    {
        public GoogleRegisterRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.IdToken)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "IdToken"));

            RuleFor(x => x.AccountType)
                .IsInEnum()
                .WithMessage(x => messageService.Get("InvalidValue", "AccountType"));

            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .When(x => x.AccountType == AccountType.CompanyAdmin)
                .WithMessage(x => messageService.Get("RequiredField", "CompanyName"))
                .MaximumLength(200)
                .WithMessage(x => messageService.Get("MaxLength", "CompanyName", 200));

            RuleFor(x => x.WhatsApp)
                .MustBeValidPhoneNumber()
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp))
                .WithMessage(x => messageService.Get("InvalidPhoneFormat"));
        }
    }
}
