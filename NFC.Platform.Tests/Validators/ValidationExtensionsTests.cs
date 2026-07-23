using FluentValidation;
using FluentValidation.TestHelper;
using NFC.Platform.Application.Validators;
using Xunit;

namespace NFC.Platform.Tests.Validators
{
    public class ValidationExtensionsTests
    {
        private class TestModel
        {
            public string? PhoneNumber { get; set; }
        }

        private class TestModelValidator : AbstractValidator<TestModel>
        {
            public TestModelValidator()
            {
                RuleFor(x => x.PhoneNumber).MustBeValidPhoneNumber();
            }
        }

        private readonly TestModelValidator _validator;

        public ValidationExtensionsTests()
        {
            _validator = new TestModelValidator();
        }

        [Theory]
        [InlineData("+201012345678")] // Standard E.164 Egypt (+20) format
        [InlineData("+14155552671")] // US (+1) format
        [InlineData("201012345678")] // Missing +, but valid digits
        [InlineData("12345678")] // 8 digits (Minimum allowed by \d{7,14})
        [InlineData("123456789012345")] // 15 digits (Maximum allowed by \d{7,14})
        [InlineData(null)] // Should pass because the field is optional (string.IsNullOrWhiteSpace)
        [InlineData("")] // Optional
        [InlineData("   ")] // Optional
        public void MustBeValidPhoneNumber_ShouldNotHaveValidationError_WhenValid(string? phoneNumber)
        {
            // Arrange
            var model = new TestModel { PhoneNumber = phoneNumber };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
        }

        [Theory]
        [InlineData("+1234567")] // Too short (7 digits, minimum is 8)
        [InlineData("+1234567890123456")] // Too long (16 digits, max is 15)
        [InlineData("020101234567")] // Cannot start with 0 if + is omitted, per ^\+?[1-9]
        [InlineData("+02010123456")] // Cannot start with 0 immediately after + per ^\+?[1-9]
        [InlineData("abcde")] // Letters
        [InlineData("+20101234a567")] // Contains letters inside digits
        [InlineData("++201012345678")] // Double plus
        [InlineData("+ 201012345678")] // Contains space
        [InlineData("2010-123-4567")] // Contains dashes
        public void MustBeValidPhoneNumber_ShouldHaveValidationError_WhenInvalid(string phoneNumber)
        {
            // Arrange
            var model = new TestModel { PhoneNumber = phoneNumber };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        }
    }
}
