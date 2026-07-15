using FluentValidation;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.Admin
{
    public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusDto>
    {
        public UpdateOrderStatusValidator(IMessageService messageService)
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage(x => messageService.Get("InvalidValue", "Status"));

            RuleFor(x => x.TrackingNumber)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "TrackingNumber"))
                .When(x => x.Status == OrderStatus.ReadyForDelivery);
        }
    }
}
