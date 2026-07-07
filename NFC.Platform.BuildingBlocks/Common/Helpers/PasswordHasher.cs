using System;
using System.Security.Cryptography;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    /// <summary>
    /// Helper class providing PBKDF2 password hashing and verification services.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128-bit salt
        private const int KeySize = 32;  // 256-bit subkey
        private const int Iterations = 100000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        private const char SegmentDelimiter = ':';

        /// <summary>
        /// Hashes the provided plain-text password using PBKDF2 with a cryptographically secure random salt.
        /// </summary>
        /// <param name="password">The plain-text password to hash.</param>
        /// <returns>A formatted string containing the iterations, salt, and password hash segments.</returns>
        /// <exception cref="ArgumentException">Thrown when password is null or empty.</exception>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                Algorithm,
                KeySize
            );

            return string.Join(
                SegmentDelimiter,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash)
            );
        }

        /// <summary>
        /// Verifies a plain-text password against a previously generated PBKDF2 password hash.
        /// </summary>
        /// <param name="password">The plain-text password to verify.</param>
        /// <param name="hashedPassword">The formatted hash string containing iterations, salt, and hash segments.</param>
        /// <returns>True if the password matches the hash; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when password or hashedPassword is null or empty.</exception>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            }

            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                throw new ArgumentException("Hashed password cannot be null or empty.", nameof(hashedPassword));
            }

            string[] segments = hashedPassword.Split(SegmentDelimiter);

            if (segments.Length != 3)
            {
                return false;
            }

            if (!int.TryParse(segments[0], out int iterations))
            {
                return false;
            }

            byte[] salt;
            byte[] hash;
            try
            {
                salt = Convert.FromBase64String(segments[1]);
                hash = Convert.FromBase64String(segments[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                Algorithm,
                hash.Length
            );

            return CryptographicOperations.FixedTimeEquals(hash, inputHash);
        }
    }
}
