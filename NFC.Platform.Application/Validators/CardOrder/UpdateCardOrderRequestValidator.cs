using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.CardOrder;

/// <summary>
/// Validates structural rules for UpdateCardOrderRequest (PendingReview only).
/// Design and payment fields are owned by CardDesign — they cannot be updated here.
/// </summary>
public class UpdateCardOrderRequestValidator : AbstractValidator<UpdateCardOrderRequest>
{
    public UpdateCardOrderRequestValidator(IMessageService messageService)
    {
        // EmployeeIds — required when SpecificEmployees
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
