using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.CardOrder;

/// <summary>
/// Validates structural rules for CreateCardOrderRequest.
/// AccountType-specific rules (AssignmentScope vs Quantity) are enforced
/// inside CardOrderService after reading AccountType from the JWT.
/// </summary>
public class CreateCardOrderRequestValidator : AbstractValidator<CreateCardOrderRequest>
{
    public CreateCardOrderRequestValidator(IMessageService messageService)
    {
        // CardDesignId — optional (auto-resolved when omitted)
        RuleFor(x => x.CardDesignId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("RequiredField",
                messageService.Get("CardDesignId")))
            .When(x => x.CardDesignId.HasValue);

        // EmployeeIds — required when AssignmentScope = SpecificEmployees
        RuleFor(x => x.EmployeeIds)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("EmployeesRequired"))
            .When(x => x.AssignmentScope == AssignmentScope.SpecificEmployees);

        // QuantityPerEmployee — if provided must be > 0
        RuleFor(x => x.QuantityPerEmployee)
            .GreaterThan(0)
            .WithMessage(_ => messageService.Get("InvalidRange",
                messageService.Get("QuantityPerEmployee"), "1", int.MaxValue.ToString()))
            .When(x => x.QuantityPerEmployee.HasValue);

        // Quantity — if provided must be > 0
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage(_ => messageService.Get("InvalidRange",
                messageService.Get("Quantity"), "1", int.MaxValue.ToString()))
            .When(x => x.Quantity.HasValue);
    }
}
