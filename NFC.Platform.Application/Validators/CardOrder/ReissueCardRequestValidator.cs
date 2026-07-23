using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardOrder
{
    public class ReissueCardRequestValidator : AbstractValidator<ReissueCardRequest>
    {
        public ReissueCardRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.ShippingAddress)
                .MaximumLength(500)
                .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("Address"), 500));
        }
    }
}
