using FluentValidation;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Subscription;

public class UpdateSubscriptionPlanRequestValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanRequestValidator(IMessageService messageService)
    {
        When(x => x.NameAr != null, () =>
        {
            RuleFor(x => x.NameAr!)
                .NotEmpty().WithMessage(messageService.Get("RequiredField", messageService.Get("NameAr")))
                .MaximumLength(100).WithMessage(messageService.Get("MaxLength", messageService.Get("NameAr"), 100));
        });

        When(x => x.NameEn != null, () =>
        {
            RuleFor(x => x.NameEn!)
                .NotEmpty().WithMessage(messageService.Get("RequiredField", messageService.Get("NameEn")))
                .MaximumLength(100).WithMessage(messageService.Get("MaxLength", messageService.Get("NameEn"), 100));
        });

        When(x => x.Price.HasValue, () =>
        {
            RuleFor(x => x.Price!.Value)
                .GreaterThanOrEqualTo(0).WithMessage(messageService.Get("ValueMustBePositive"));
        });

        When(x => x.DurationInDays.HasValue, () =>
        {
            RuleFor(x => x.DurationInDays!.Value)
                .GreaterThan(0).WithMessage(messageService.Get("ValueMustBePositive"));
        });
    }
}
