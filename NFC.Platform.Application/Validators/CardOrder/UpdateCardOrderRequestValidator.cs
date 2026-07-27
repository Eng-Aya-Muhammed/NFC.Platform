using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.CardOrder;

public class UpdateCardOrderRequestValidator : AbstractValidator<UpdateCardOrderRequest>
{
    public UpdateCardOrderRequestValidator(IMessageService messageService)
    {
        RuleFor(x => x.CardDesignType)
            .IsInEnum()
            .WithMessage(_ => messageService.Get("CardDesignTypeRequired"))
            .When(x => x.CardDesignType.HasValue);

        RuleFor(x => x.CardTypeId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("InvalidOrInactiveCardType"))
            .When(x => x.CardTypeId.HasValue);

        RuleFor(x => x.CardPackageId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("InvalidOrInactiveCardPackage"))
            .When(x => x.CardPackageId.HasValue);

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage(_ => messageService.Get("QuantityRequired"))
            .LessThanOrEqualTo(10000)
            .WithMessage(_ => messageService.Get("QuantityRequired"))
            .Must((request, quantity) => !request.AssignmentScope.HasValue || 
                                         request.AssignmentScope != AssignmentScope.SpecificEmployees || 
                                         request.EmployeeIds == null || 
                                         request.EmployeeIds.Count == quantity)
            .WithMessage(x => messageService.Get("EmployeeCountMismatch", x.EmployeeIds != null ? x.EmployeeIds.Count.ToString() : "0", x.Quantity.ToString()))
            .When(x => x.Quantity.HasValue);

        RuleFor(x => x.ExcelDataUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Excel Data URL"))
            .When(x => !string.IsNullOrWhiteSpace(x.ExcelDataUrl));

        RuleFor(x => x.DeliveryMethod)
            .IsInEnum()
            .When(x => x.DeliveryMethod.HasValue);

        RuleFor(x => x.ShippingAddress)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("ShippingAddressRequired"))
            .When(x => x.DeliveryMethod == DeliveryMethod.Courier);

        RuleFor(x => x.FrontDesignUrl)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("FrontDesignRequired"))
            .When(x => x.CardDesignType == CardDesignType.CustomArtwork);

        RuleFor(x => x.FrontDesignUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Front Design URL"))
            .When(x => !string.IsNullOrWhiteSpace(x.FrontDesignUrl));

        RuleFor(x => x.BackDesignUrl)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("BackDesignRequired"))
            .When(x => x.CardDesignType == CardDesignType.CustomArtwork);

        RuleFor(x => x.BackDesignUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Back Design URL"))
            .When(x => !string.IsNullOrWhiteSpace(x.BackDesignUrl));
    }
}
