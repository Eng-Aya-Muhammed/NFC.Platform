using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardOrder
{
    public class ReorderRequestValidator : AbstractValidator<ReorderRequest>
    {
        public ReorderRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage(_ => messageService.Get("QuantityRequired"))
                .LessThanOrEqualTo(10000)
                .WithMessage(_ => messageService.Get("QuantityRequired"));

            RuleFor(x => x.AssignmentScope)
                .IsInEnum()
                .WithMessage(_ => messageService.Get("InvalidValue", "AssignmentScope"));

            RuleFor(x => x.ShippingAddress)
                .MaximumLength(500)
                .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("Address"), 500));
        }
    }
}
