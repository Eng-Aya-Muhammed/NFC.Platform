using FluentValidation;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Validators.Card;

    public class CreateCardRequestValidator : AbstractValidator<CreateCardRequest>
    {
        public CreateCardRequestValidator(IMessageService messageService)
        {
            RuleFor(x => x.ActivationCode)
                .NotEmpty()
                .WithMessage(x => messageService.Get("RequiredField", "ActivationCode"))
                .MaximumLength(100)
                .WithMessage(x => messageService.Get("MaxLength", "ActivationCode", 100));
        }
    }

