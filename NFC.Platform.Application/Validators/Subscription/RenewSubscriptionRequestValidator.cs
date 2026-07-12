using System;
using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Subscription
{
    public class RenewSubscriptionRequestValidator : AbstractValidator<RenewSubscriptionRequest>
    {
        public RenewSubscriptionRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.SubscriptionPlanId)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "SubscriptionPlanId"));
        }
    }
}
