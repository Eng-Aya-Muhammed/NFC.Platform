using FluentValidation;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.DiscountCode;

public class ValidateDiscountCodeRequestValidator : AbstractValidator<ValidateDiscountCodeRequest>
{
    public ValidateDiscountCodeRequestValidator(IMessageService messageService)
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(messageService.Get("FieldRequired"));

        RuleFor(x => x.OrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage(messageService.Get("ValueMustBePositive"));
    }
}
