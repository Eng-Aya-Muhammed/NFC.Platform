using NFC.Platform.BuildingBlocks.Common.Helpers;
using Xunit;

namespace NFC.Platform.Tests.Helpers
{
    public class OtpHasherTests
    {
        [Fact]
        public void HashOtp_ReturnsNonNullHash_ForValidOtpCode()
        {
            var otp = "123456";

            var hash = OtpHasher.HashOtp(otp);

            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.Equal(64, hash.Length);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HashOtp_ReturnsEmptyString_ForNullOrEmptyInput(string? input)
        {
            var hash = OtpHasher.HashOtp(input);

            Assert.Equal(string.Empty, hash);
        }

        [Fact]
        public void VerifyOtp_ReturnsTrue_WhenInputMatchesStoredHash()
        {
            var otp = "654321";
            var hash = OtpHasher.HashOtp(otp);

            var isValid = OtpHasher.VerifyOtp(otp, hash);

            Assert.True(isValid);
        }

        [Fact]
        public void VerifyOtp_ReturnsFalse_WhenInputDoesNotMatchStoredHash()
        {
            var otp = "654321";
            var hash = OtpHasher.HashOtp(otp);

            var isValid = OtpHasher.VerifyOtp("999999", hash);

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null, "hash")]
        [InlineData("123456", null)]
        [InlineData("", "")]
        public void VerifyOtp_ReturnsFalse_WhenInputOrHashIsNullOrEmpty(string? inputOtp, string? storedHash)
        {
            var isValid = OtpHasher.VerifyOtp(inputOtp, storedHash);

            Assert.False(isValid);
        }
    }
}
