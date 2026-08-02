using System;
using System.Security.Cryptography;
using System.Text;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    /// <summary>
    /// Cryptographic helper utility for securely hashing and verifying One-Time Passwords (OTPs).
    /// Ensures plaintext OTPs are never stored in database tables and prevents timing attacks.
    /// </summary>
    public static class OtpHasher
    {
        private const string OtpSaltPepper = "NFC_Platform_Secure_OTP_Salt_2026";

        /// <summary>
        /// Generates a SHA-256 cryptographic hash of a plaintext OTP code combined with salt.
        /// </summary>
        /// <param name="otpCode">The plaintext OTP code.</param>
        /// <returns>A uppercase hexadecimal hash string.</returns>
        public static string HashOtp(string? otpCode)
        {
            if (string.IsNullOrWhiteSpace(otpCode))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var saltedBytes = Encoding.UTF8.GetBytes($"{otpCode}_{OtpSaltPepper}");
            var hashBytes = sha256.ComputeHash(saltedBytes);
            return Convert.ToHexString(hashBytes);
        }

        /// <summary>
        /// Verifies a plaintext OTP code against a stored hash using constant-time comparison.
        /// </summary>
        /// <param name="inputOtp">The input OTP code provided by the user.</param>
        /// <param name="storedHash">The stored cryptographic hash from the database.</param>
        /// <returns>True if the hashes match; otherwise false.</returns>
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
