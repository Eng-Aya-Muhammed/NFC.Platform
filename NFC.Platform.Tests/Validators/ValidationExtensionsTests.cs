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
        [InlineData("+201012345678")]
        [InlineData("+14155552671")]
        [InlineData("201012345678")]
        [InlineData("12345678")]
        [InlineData("123456789012345")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void MustBeValidPhoneNumber_ShouldNotHaveValidationError_WhenValid(string? phoneNumber)
        {
            var model = new TestModel { PhoneNumber = phoneNumber };

            var result = _validator.TestValidate(model);

            result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
        }

        [Theory]
        [InlineData("+1234567")]
        [InlineData("+1234567890123456")]
        [InlineData("020101234567")]
        [InlineData("+02010123456")]
        [InlineData("abcde")]
        [InlineData("+20101234a567")]
        [InlineData("++201012345678")]
        [InlineData("+ 201012345678")]
        [InlineData("2010-123-4567")]
        public void MustBeValidPhoneNumber_ShouldHaveValidationError_WhenInvalid(string phoneNumber)
        {
            var model = new TestModel { PhoneNumber = phoneNumber };

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        }
    }
}
