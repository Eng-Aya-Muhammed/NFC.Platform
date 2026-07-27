using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.CardOrder;

public class CreateCardOrderRequestValidator : AbstractValidator<CreateCardOrderRequest>
{
    public CreateCardOrderRequestValidator(IMessageService messageService)
    {
        RuleFor(x => x.CardDesignType)
            .IsInEnum()
            .WithMessage(_ => messageService.Get("CardDesignTypeRequired"));

        RuleFor(x => x.CardTypeId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("InvalidOrInactiveCardType"));

        RuleFor(x => x.CardPackageId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("InvalidOrInactiveCardPackage"));

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage(_ => messageService.Get("QuantityRequired"))
            .LessThanOrEqualTo(10000)
            .WithMessage(_ => messageService.Get("QuantityRequired"));

        RuleFor(x => x.ExcelDataUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Excel Data URL"))
            .When(x => !string.IsNullOrWhiteSpace(x.ExcelDataUrl));

        RuleFor(x => x.DeliveryMethod)
            .IsInEnum();

        RuleFor(x => x.ShippingAddress)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("ShippingAddressRequired"))
            .When(x => x.DeliveryMethod == DeliveryMethod.Courier);

        When(x => x.CardDesignType == CardDesignType.CustomArtwork, () =>
        {
            RuleFor(x => x.FrontDesignUrl)
                .NotEmpty()
                .WithMessage(_ => messageService.Get("FrontDesignRequired"))
                .MustBeValidUrl()
                .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Front Design URL"));

            RuleFor(x => x.BackDesignUrl)
                .NotEmpty()
                .WithMessage(_ => messageService.Get("BackDesignRequired"))
                .MustBeValidUrl()
                .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Back Design URL"));
        });
    }
}
