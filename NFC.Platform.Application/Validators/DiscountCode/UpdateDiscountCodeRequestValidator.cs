using FluentValidation;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.DiscountCode;

public class UpdateDiscountCodeRequestValidator : AbstractValidator<UpdateDiscountCodeRequest>
{
    public UpdateDiscountCodeRequestValidator(IMessageService messageService)
    {
        When(x => x.Code != null, () =>
        {
            RuleFor(x => x.Code!)
                .NotEmpty().WithMessage(messageService.Get("RequiredField", messageService.Get("Code")))
                .MaximumLength(50).WithMessage(messageService.Get("MaxLength", messageService.Get("Code"), 50));
        });

        When(x => x.DiscountValue.HasValue, () =>
        {
            RuleFor(x => x.DiscountValue!.Value)
                .GreaterThan(0).WithMessage(messageService.Get("ValueMustBePositive"));
        });

        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate!.Value)
                .GreaterThan(x => x.StartDate!.Value).WithMessage(messageService.Get("EndDateMustBeAfterStartDate"));
        });
    }
}
