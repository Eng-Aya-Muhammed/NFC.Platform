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
                .When(x => x.CardDesignType.HasValue)
                .WithMessage(x => messageService.Get("CardDesignTypeRequired") ?? "Card design type is required.");

            When(x => x.CardDesignType == CardDesignType.CustomArtwork, () =>
            {
                RuleFor(x => x.FrontDesignUrl)
                    .NotEmpty()
                    .WithMessage(x => messageService.Get("FrontDesignRequired") ?? "Front design file is required.");

                RuleFor(x => x.BackDesignUrl)
                    .NotEmpty()
                    .WithMessage(x => messageService.Get("BackDesignRequired") ?? "Back design file is required.");
            });

            When(x => x.CardDesignType == CardDesignType.NeedCustomDesign, () =>
            {
                RuleFor(x => x.LogoUrl)
                    .NotEmpty()
                    .WithMessage(x => messageService.Get("LogoRequiredForCustomDesign") ?? "Logo is required for custom design requests.");
            });

            When(x => x.AssignmentScope == AssignmentScope.SpecificEmployees, () =>
            {
                RuleFor(x => x.EmployeeIds)
                    .NotEmpty()
                    .WithMessage(x => messageService.Get("EmployeeIdsRequiredForSpecificAssignment") ?? "Employee IDs are required when assigning to specific employees.");

                RuleFor(x => x)
                    .Must(x => x.EmployeeIds != null && x.EmployeeIds.Count == x.Quantity)
                    .WithMessage(x => messageService.Get("EmployeeCountMismatch", (x.EmployeeIds?.Count ?? 0).ToString(), x.Quantity.ToString())
                        ?? $"Quantity ({x.Quantity}) does not match the number of assigned employees ({(x.EmployeeIds?.Count ?? 0)}).");
            });
        }
    }
}
