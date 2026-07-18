using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Validators.CardOrder;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Application.Services;
using NFC.Platform.Application.Interfaces.Repositories;
using NSubstitute;
using Xunit;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Application.DTOs.Admin;

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
        public void Validator_ShouldPass_WhenExactlyOneDesignSourceIsPresent_FrontDesignUrl()
        {
            // Arrange — physical design is now sourced only from uploaded URLs or a custom design request
            var request = new CreateCardOrderRequest
            {
                FrontDesignUrl = "https://cdn.example.com/front.png",
                Quantity = 1
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validator_ShouldPass_WhenExactlyOneDesignSourceIsPresent_CustomDesignRequestId()
        {
            // Arrange
            var request = new CreateCardOrderRequest
            {
                CustomDesignRequestId = Guid.NewGuid(),
                Quantity = 1
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validator_ShouldFail_WhenZeroDesignSourcesArePresent()
        {
            // Arrange
            var request = new CreateCardOrderRequest
            {
                Quantity = 1
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validator_ShouldFail_WhenMultipleDesignSourcesArePresent()
        {
            // Arrange — FrontDesignUrl and CustomDesignRequestId are mutually exclusive
            var request = new CreateCardOrderRequest
            {
                FrontDesignUrl = "https://cdn.example.com/front.png",
                CustomDesignRequestId = Guid.NewGuid(),
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

            var storageService = Substitute.For<NFC.Platform.Application.Interfaces.Services.IStorageService>();
            var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();
            var service = new CardOrderService(unitOfWork, mapper, messageService, currentTenant, excelParser, validator, storageService, backgroundJobClient);
            var request = new CreateCardOrderRequest { Quantity = 1 };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }
    }
}
