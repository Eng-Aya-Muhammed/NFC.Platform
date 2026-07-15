using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardOrder
{
    public class CreateCardOrderRequestValidator : AbstractValidator<CreateCardOrderRequest>
    {
        public CreateCardOrderRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x)
                .Must(x =>
                {
                    int count = 0;
                    if (x.PrintTemplateId.HasValue) count++;
                    if (!string.IsNullOrWhiteSpace(x.FrontDesignUrl) || !string.IsNullOrWhiteSpace(x.BackDesignUrl)) count++;
                    if (x.CustomDesignRequestId.HasValue) count++;
                    return count == 1;
                })
                .WithMessage(x => messageService.Get("ExactlyOneDesignSourceRequired") ?? "Exactly one design source must be specified (Template, Uploaded Files, or Custom Design).");
        }
    }
}
