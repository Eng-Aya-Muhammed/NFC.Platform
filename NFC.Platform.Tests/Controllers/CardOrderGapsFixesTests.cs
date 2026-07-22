using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.Settings;

namespace NFC.Platform.Tests.Controllers
{
    public class CardOrderGapsFixesTests
    {
        private readonly IMessageService _messageService;
        private readonly CreateCardOrderRequestValidator _validator;

        public CardOrderGapsFixesTests()
        {
            _messageService = Substitute.For<IMessageService>();
            _messageService.Get(Arg.Any<string>()).Returns("Validation Error");
            _validator = new CreateCardOrderRequestValidator(_messageService);
        }

        [Fact]
        public void Validator_ShouldPass_WhenCustomArtwork_HasFrontAndBackDesignUrl()
        {
            var request = new CreateCardOrderRequest
            {
                CardType = CardType.Plastic,
                CardDesignType = CardDesignType.CustomArtwork,
                FrontDesignUrl = "https://cdn.example.com/front.png",
                BackDesignUrl = "https://cdn.example.com/back.png",
                Quantity = 1
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.True(result.IsValid);
        }



        [Fact]
        public void Validator_ShouldFail_WhenCustomArtwork_MissingFrontOrBackDesignUrl()
        {
            // Arrange
            var request = new CreateCardOrderRequest
            {
                CardType = CardType.Plastic,
                CardDesignType = CardDesignType.CustomArtwork,
                FrontDesignUrl = "https://cdn.example.com/front.png",
                Quantity = 1
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.False(result.IsValid);
        }



        [Fact]
        public async Task CreateAsync_ShouldReturn422_WhenValidationFails()
        {
            // Arrange
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var mapper = Substitute.For<IMapper>();
            var messageService = Substitute.For<IMessageService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var excelParser = Substitute.For<IExcelParser>();
            
            var validator = Substitute.For<IValidator<CreateCardOrderRequest>>();
            var validationFailures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("DesignSource", "Exactly one design source must be specified.")
            };
            var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(validationResult));

            var cardPricingService = Substitute.For<ICardPricingService>();
            var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();
            var otpSettingsOptions = Substitute.For<IOptions<OtpSettings>>();
            otpSettingsOptions.Value.Returns(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });
            var service = new CardOrderService(unitOfWork, mapper, messageService, currentTenant, cardPricingService, validator, backgroundJobClient, otpSettingsOptions);
            var request = new CreateCardOrderRequest { Quantity = 1 };

            // Act
            var result = await service.CreateOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }
    }
}
