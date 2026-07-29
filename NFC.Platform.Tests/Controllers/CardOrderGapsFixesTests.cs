using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.Settings;
using NFC.Platform.Application.Validators.CardDesign;

namespace NFC.Platform.Tests.Controllers
{
    public class CardOrderGapsFixesTests
    {
        private readonly IMessageService _messageService;
        private readonly CreateCardDesignRequestValidator _designValidator;
        private readonly CreateCardOrderRequestValidator _orderValidator;

        public CardOrderGapsFixesTests()
        {
            _messageService = Substitute.For<IMessageService>();
            _messageService.Get(Arg.Any<string>()).Returns("Validation Error");
            _designValidator = new CreateCardDesignRequestValidator(_messageService);
            _orderValidator = new CreateCardOrderRequestValidator(_messageService);
        }

        [Fact]
        public void DesignValidator_ShouldPass_WhenCustomArtwork_HasFrontAndBackDesignUrl()
        {
            var request = new CreateCardDesignRequest
            {
                CardTypeId = Guid.NewGuid(),
                CardPackageId = Guid.NewGuid(),
                CardDesignType = CardDesignType.CustomArtwork,
                FrontDesignUrl = "https://cdn.example.com/front.png",
                BackDesignUrl = "https://cdn.example.com/back.png",
            };

            // Act
            var result = _designValidator.Validate(request);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void DesignValidator_ShouldFail_WhenCustomArtwork_MissingFrontOrBackDesignUrl()
        {
            // Arrange
            var request = new CreateCardDesignRequest
            {
                CardTypeId = Guid.NewGuid(),
                CardPackageId = Guid.NewGuid(),
                CardDesignType = CardDesignType.CustomArtwork,
                FrontDesignUrl = "https://cdn.example.com/front.png",
            };

            // Act
            var result = _designValidator.Validate(request);

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
            
            var validator = Substitute.For<IValidator<CreateCardOrderRequest>>();
            var validationFailures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("CardDesignId", "Card design is required.")
            };
            var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(validationResult));

            var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();
            var otpSettingsOptions = Substitute.For<IOptions<OtpSettings>>();
            otpSettingsOptions.Value.Returns(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });
            var service = new CardOrderService(unitOfWork, mapper, messageService, currentTenant, validator, Substitute.For<IValidator<UpdateCardOrderRequest>>(), backgroundJobClient, Substitute.For<IEmployeeService>(), otpSettingsOptions);
            var request = new CreateCardOrderRequest { CardDesignId = Guid.NewGuid() };

            // Act
            var result = await service.CreateOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }
    }
}
