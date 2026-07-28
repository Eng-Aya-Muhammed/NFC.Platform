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

        RuleFor(x => x.EmployeeIds)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("EmployeesRequired"))
            .When(x => x.AssignmentScope == AssignmentScope.SpecificEmployees);

        RuleFor(x => x.ExcelDataUrl)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("ExcelUrlRequired"))
            .When(x => x.AssignmentScope == AssignmentScope.ExcelUpload);

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
