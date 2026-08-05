using System;
using System.Security.Cryptography;
using System.Text;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public static class OtpHasher
    {
        private const string OtpSaltPepper = "NFC_Platform_Secure_OTP_Salt_2026";

        public static string HashOtp(string? otpCode)
        {
            if (string.IsNullOrWhiteSpace(otpCode))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var saltedBytes = Encoding.UTF8.GetBytes($"{otpCode}_{OtpSaltPepper}");
            var hashBytes = sha256.ComputeHash(saltedBytes);
            return Convert.ToHexString(hashBytes);
        }

        public static bool VerifyOtp(string? inputOtp, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(inputOtp) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            var inputHash = HashOtp(inputOtp);
            var inputHashBytes = Encoding.UTF8.GetBytes(inputHash);
            var storedHashBytes = Encoding.UTF8.GetBytes(storedHash);

            if (inputHashBytes.Length != storedHashBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(inputHashBytes, storedHashBytes);
        }
    }
}
