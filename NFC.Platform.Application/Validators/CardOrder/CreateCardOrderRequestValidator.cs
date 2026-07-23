using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.CardOrder
{
    public class CreateCardOrderRequestValidator : AbstractValidator<CreateCardOrderRequest>
    {
        public CreateCardOrderRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.CardDesignType)
                .IsInEnum()
                .WithMessage(_ => messageService.Get("CardDesignTypeRequired"));

            RuleFor(x => x.CardType)
                .IsInEnum()
                .WithMessage(_ => messageService.Get("CardTypeRequired"));

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage(_ => messageService.Get("QuantityRequired"))
                .LessThanOrEqualTo(10000)
                .WithMessage(_ => messageService.Get("QuantityRequired"));

            RuleFor(x => x.ExcelDataUrl)
                .MustBeValidUrl()
                .WithMessage(_ => messageService.Get("InvalidUrlFormat", "Excel Data URL"));


            // When the customer has their own ready design → front & back files are mandatory
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
}

