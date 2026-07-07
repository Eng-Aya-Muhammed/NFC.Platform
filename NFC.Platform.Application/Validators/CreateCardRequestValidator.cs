using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators
{
    public class CreateCardRequestValidator : AbstractValidator<CreateCardRequest>
    {
        public CreateCardRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "CardNumber"))
                .MaximumLength(50)
                .WithMessage(x => messageService.Get("MaxLength", "CardNumber", 50));

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "Name"))
                .MaximumLength(100)
                .WithMessage(x => messageService.Get("MaxLength", "Name", 100));

            RuleFor(x => x.Price)
                .ExclusiveBetween(0.01m, 1000000.00m)
                .WithMessage(x => messageService.Get("InvalidRange", "Price", 0.01, 1000000.00));
        }
    }
}
