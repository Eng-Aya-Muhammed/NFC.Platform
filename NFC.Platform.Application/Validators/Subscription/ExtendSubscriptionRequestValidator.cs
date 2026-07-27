using FluentValidation;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Subscription
{
    public class ExtendSubscriptionRequestValidator : AbstractValidator<ExtendSubscriptionRequest>
    {
        public ExtendSubscriptionRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.ExtensionDays)
                .InclusiveBetween(1, 3650)
                .WithMessage(x => messageService.Get("InvalidExtensionDays"));
        }
    }
}
