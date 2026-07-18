using System;
using NFC.Platform.Infrastructure.Services;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="QrCodeGeneratorService"/>.
    /// These tests do NOT require network access or Cloudinary credentials —
    /// they only verify in-process PNG byte generation.
    /// </summary>
    public class QrCodeGeneratorTests
    {
        private static readonly byte[] PngMagicBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private readonly QrCodeGeneratorService _sut = new();

        // ── Basic contract ────────────────────────────────────────────────────────

        [Fact]
        public void GeneratePngBytes_ReturnsNonEmptyByteArray()
        {
            // Act
            var result = _sut.GeneratePngBytes("https://onpoint-teasting.com/c/ABC12345");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GeneratePngBytes_ReturnsByteArray_WithValidPngMagicHeader()
        {
            // Act
            var result = _sut.GeneratePngBytes("https://onpoint-teasting.com/c/ABC12345");

            // Assert — PNG files always start with the 8-byte PNG signature.
            Assert.True(result.Length >= 8, "PNG byte array must be at least 8 bytes (magic header).");
            var header = result[..8];
            Assert.Equal(PngMagicBytes, header);
        }

        [Fact]
        public void GeneratePngBytes_DifferentUrls_ProduceDifferentImages()
        {
            // Arrange — two different card codes produce different QR data.
            var bytes1 = _sut.GeneratePngBytes("https://onpoint-teasting.com/c/AAAAAA0001");
            var bytes2 = _sut.GeneratePngBytes("https://onpoint-teasting.com/c/BBBBBB0002");

            // Act + Assert — the outputs must differ (QR data is URL-specific).
            Assert.False(bytes1.SequenceEqual(bytes2), "Different URLs should produce different QR code images.");
        }

        [Fact]
        public void GeneratePngBytes_SameUrl_ProducesIdenticalImages()
        {
            // Two calls with the same URL should be deterministic.
            const string url = "https://onpoint-teasting.com/c/CONSISTENT01";
            var bytes1 = _sut.GeneratePngBytes(url);
            var bytes2 = _sut.GeneratePngBytes(url);

            Assert.Equal(bytes1, bytes2);
        }

        // ── pixel-size parameter ──────────────────────────────────────────────────

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(20)]
        public void GeneratePngBytes_AcceptsVariousPixelSizes_WithoutThrowing(int pixelSize)
        {
            // Verify that the API surface accepts the pixelSize parameter without exception.
            var result = _sut.GeneratePngBytes("https://onpoint-teasting.com/c/XTEST", pixelSize);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GeneratePngBytes_LargerPixelSize_ProducesLargerImage()
        {
            // A larger pixel size multiplies module size → PNG output is bigger.
            var smallBytes = _sut.GeneratePngBytes("https://onpoint-teasting.com/c/SIZE", pixelSize: 3);
            var largeBytes = _sut.GeneratePngBytes("https://onpoint-teasting.com/c/SIZE", pixelSize: 15);

            Assert.True(largeBytes.Length > smallBytes.Length,
                "Larger pixelSize should produce a larger PNG byte array.");
        }

        // ── edge inputs ───────────────────────────────────────────────────────────

        [Fact]
        public void GeneratePngBytes_VeryShortUrl_DoesNotThrow()
        {
            // Minimum realistic URL: domain/c/code (still a valid QR payload).
            var result = _sut.GeneratePngBytes("https://a.co/c/X");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GeneratePngBytes_LongUrl_DoesNotThrow()
        {
            // Simulate an unusually long URL (QR can encode up to ~4,000 chars at low ECC).
            var longCode = new string('A', 200);
            var url = $"https://onpoint-teasting.com/c/{longCode}";
            var result = _sut.GeneratePngBytes(url);
            Assert.NotEmpty(result);
        }
    }
}
