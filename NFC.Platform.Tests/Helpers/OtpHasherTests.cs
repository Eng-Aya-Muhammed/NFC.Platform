using NFC.Platform.BuildingBlocks.Common.Helpers;
using Xunit;

namespace NFC.Platform.Tests.Helpers
{
    public class OtpHasherTests
    {
        [Fact]
        public void HashOtp_ReturnsNonNullHash_ForValidOtpCode()
        {
            // Arrange
            var otp = "123456";

            // Act
            var hash = OtpHasher.HashOtp(otp);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.Equal(64, hash.Length); // SHA-256 hex string length
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HashOtp_ReturnsEmptyString_ForNullOrEmptyInput(string? input)
        {
            // Act
            var hash = OtpHasher.HashOtp(input);

            // Assert
            Assert.Equal(string.Empty, hash);
        }

        [Fact]
        public void VerifyOtp_ReturnsTrue_WhenInputMatchesStoredHash()
        {
            // Arrange
            var otp = "654321";
            var hash = OtpHasher.HashOtp(otp);

            // Act
            var isValid = OtpHasher.VerifyOtp(otp, hash);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void VerifyOtp_ReturnsFalse_WhenInputDoesNotMatchStoredHash()
        {
            // Arrange
            var otp = "654321";
            var hash = OtpHasher.HashOtp(otp);

            // Act
            var isValid = OtpHasher.VerifyOtp("999999", hash);

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null, "hash")]
        [InlineData("123456", null)]
        [InlineData("", "")]
        public void VerifyOtp_ReturnsFalse_WhenInputOrHashIsNullOrEmpty(string? inputOtp, string? storedHash)
        {
            // Act
            var isValid = OtpHasher.VerifyOtp(inputOtp, storedHash);

            // Assert
            Assert.False(isValid);
        }
    }
}
