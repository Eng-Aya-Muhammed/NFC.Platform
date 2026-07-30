using FluentValidation;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Subscription;

public class CreateSubscriptionPlanRequestValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanRequestValidator(IMessageService messageService)
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage(messageService.Get("RequiredField", messageService.Get("NameAr")))
            .MaximumLength(100).WithMessage(messageService.Get("MaxLength", messageService.Get("NameAr"), 100));

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage(messageService.Get("RequiredField", messageService.Get("NameEn")))
            .MaximumLength(100).WithMessage(messageService.Get("MaxLength", messageService.Get("NameEn"), 100));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage(messageService.Get("ValueMustBePositive"));

        RuleFor(x => x.DurationInDays)
            .GreaterThan(0).WithMessage(messageService.Get("ValueMustBePositive"));
    }
}
