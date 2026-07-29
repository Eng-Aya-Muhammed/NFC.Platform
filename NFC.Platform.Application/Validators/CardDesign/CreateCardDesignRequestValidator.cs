using FluentValidation;
using NFC.Platform.Application.DTOs.CardDesign;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.CardDesign;

/// <summary>
/// Validates structural rules for CreateCardDesignRequest.
/// AccountType-specific rules (CardPackageId vs CustomQuantity) are enforced
/// inside CardDesignService after reading AccountType from the JWT.
/// </summary>
public class CreateCardDesignRequestValidator : AbstractValidator<CreateCardDesignRequest>
{
    public CreateCardDesignRequestValidator(IMessageService messageService)
    {
        // CardDesignType — required and must be a valid enum value
        RuleFor(x => x.CardDesignType)
            .IsInEnum()
            .WithMessage(_ => messageService.Get("CardDesignTypeRequired"));

        // CardTypeId — required
        RuleFor(x => x.CardTypeId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("RequiredField", messageService.Get("CardTypeId")));

        // Design URLs — required and valid when CustomArtwork is chosen
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

        // ExcelDataUrl — validate URL format if present (Company-only; ignored for Individual)
        RuleFor(x => x.ExcelDataUrl)
            .MustBeValidUrl()
            .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Excel Data URL"))
            .When(x => !string.IsNullOrWhiteSpace(x.ExcelDataUrl));

        // CustomQuantity — if provided must be > 0
        RuleFor(x => x.CustomQuantity)
            .GreaterThan(0)
            .WithMessage(_ => messageService.Get("InvalidRange",
                messageService.Get("CustomQuantity"), "1", int.MaxValue.ToString()))
            .When(x => x.CustomQuantity.HasValue);
    }
}
