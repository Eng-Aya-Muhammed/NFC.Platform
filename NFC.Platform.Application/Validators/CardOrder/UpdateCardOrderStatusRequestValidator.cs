using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardOrder
{
    public class UpdateCardOrderStatusRequestValidator : AbstractValidator<UpdateCardOrderStatusRequest>
    {
        public UpdateCardOrderStatusRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage(_ => messageService.Get("InvalidValue", messageService.Get("Status")));
        }
    }
}
