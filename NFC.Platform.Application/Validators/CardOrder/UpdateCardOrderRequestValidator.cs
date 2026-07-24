using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardOrder
{
    public class UpdateCardOrderRequestValidator : AbstractValidator<UpdateCardOrderRequest>
    {
        public UpdateCardOrderRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.CardDesignType)
                .IsInEnum()
                .WithMessage(_ => messageService.Get("CardDesignTypeRequired"))
                .When(x => x.CardDesignType.HasValue);

            RuleFor(x => x.CardType)
                .IsInEnum()
                .WithMessage(_ => messageService.Get("CardTypeRequired"))
                .When(x => x.CardType.HasValue);

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage(_ => messageService.Get("QuantityRequired"))
                .LessThanOrEqualTo(10000)
                .WithMessage(_ => messageService.Get("QuantityRequired"))
                .Must((request, quantity) => !request.AssignmentScope.HasValue || 
                                             request.AssignmentScope != Domain.Enums.AssignmentScope.SpecificEmployees || 
                                             request.EmployeeIds == null || 
                                             request.EmployeeIds.Count == quantity)
                .WithMessage(x => messageService.Get("EmployeeCountMismatch", x.Quantity.ToString()))
                .When(x => x.Quantity.HasValue);

            RuleFor(x => x.ExcelDataUrl)
                .MustBeValidUrl()
                .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Excel Data URL"))
                .When(x => !string.IsNullOrWhiteSpace(x.ExcelDataUrl));

            RuleFor(x => x.FrontDesignUrl)
                .NotEmpty()
                .WithMessage(_ => messageService.Get("FrontDesignRequired"))
                .When(x => x.CardDesignType == Domain.Enums.CardDesignType.CustomArtwork);

            RuleFor(x => x.FrontDesignUrl)
                .MustBeValidUrl()
                .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Front Design URL"))
                .When(x => !string.IsNullOrWhiteSpace(x.FrontDesignUrl));

            RuleFor(x => x.BackDesignUrl)
                .NotEmpty()
                .WithMessage(_ => messageService.Get("BackDesignRequired"))
                .When(x => x.CardDesignType == Domain.Enums.CardDesignType.CustomArtwork);

            RuleFor(x => x.BackDesignUrl)
                .MustBeValidUrl()
                .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Back Design URL"))
                .When(x => !string.IsNullOrWhiteSpace(x.BackDesignUrl));
        }
    }
}
