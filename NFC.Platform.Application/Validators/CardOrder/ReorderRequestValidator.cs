using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardOrder
{
    public class ReorderRequestValidator : AbstractValidator<ReorderRequest>
    {
        public ReorderRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.CardPackageId)
                .NotEmpty()
                .WithMessage(_ => messageService.Get("InvalidOrInactiveCardPackage"))
                .When(x => x.CardPackageId.HasValue);

            RuleFor(x => x.AssignmentScope)
                .IsInEnum()
                .WithMessage(_ => messageService.Get("InvalidValue", "AssignmentScope"));

            RuleFor(x => x.ShippingAddress)
                .MaximumLength(500)
                .WithMessage(_ => messageService.Get("MaxLength", messageService.Get("Address"), 500));
        }
    }
}
