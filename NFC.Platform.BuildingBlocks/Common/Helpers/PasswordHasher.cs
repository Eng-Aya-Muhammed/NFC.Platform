using System;
using System.Security.Cryptography;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        private const char SegmentDelimiter = ':';

        public static string HashPassword(string password)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(password, nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
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

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(password, nameof(password));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(hashedPassword, nameof(hashedPassword));

            var segments = hashedPassword.Split(SegmentDelimiter);

            if (segments.Length != 3)
            {
                return false;
            }

            if (!int.TryParse(segments[0], out var iterations))
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

            var inputHash = Rfc2898DeriveBytes.Pbkdf2(
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
