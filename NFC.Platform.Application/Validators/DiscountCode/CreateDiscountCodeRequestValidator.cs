using FluentValidation;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.DiscountCode;

public class CreateDiscountCodeRequestValidator : AbstractValidator<CreateDiscountCodeRequest>
{
    public CreateDiscountCodeRequestValidator(IMessageService messageService)
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(messageService.Get("FieldRequired"))
            .MaximumLength(50).WithMessage(messageService.Get("MaxLengthExceeded"));

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage(messageService.Get("ValueMustBePositive"));

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage(messageService.Get("EndDateMustBeAfterStartDate"));
    }
}
