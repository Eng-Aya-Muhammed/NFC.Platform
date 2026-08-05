using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Validators.CardOrder;

public class CreateCardOrderRequestValidator : AbstractValidator<CreateCardOrderRequest>
{
    public CreateCardOrderRequestValidator(IMessageService messageService)
    {
        RuleFor(x => x.CardDesignId)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("RequiredField",
                messageService.Get("CardDesignId")))
            .When(x => x.CardDesignId.HasValue);

        RuleFor(x => x.EmployeeIds)
            .NotEmpty()
            .WithMessage(_ => messageService.Get("EmployeesRequired"))
            .When(x => x.AssignmentScope == AssignmentScope.SpecificEmployees);

        RuleFor(x => x.QuantityPerEmployee)
            .GreaterThan(0)
            .WithMessage(_ => messageService.Get("InvalidRange",
                messageService.Get("QuantityPerEmployee"), "1", int.MaxValue.ToString()))
            .When(x => x.QuantityPerEmployee.HasValue);

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage(_ => messageService.Get("InvalidRange",
                messageService.Get("Quantity"), "1", int.MaxValue.ToString()))
            .When(x => x.Quantity.HasValue);
    }
}
