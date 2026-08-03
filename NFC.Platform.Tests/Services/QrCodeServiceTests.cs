using System;
using Xunit;
using NFC.Platform.Infrastructure.Services;

namespace NFC.Platform.Tests.Services;

public class QrCodeServiceTests
{
    private readonly QrCodeService _sut = new();

    [Fact]
    public void GeneratePngQrCode_ReturnsValidPngByteArray_WhenContentIsValid()
    {
        // Arrange
        var content = "https://nfc-platform.com/u/ahmed-ali";

        // Act
        var result = _sut.GeneratePngQrCode(content);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify PNG Magic Bytes header: 0x89 'P' 'N' 'G'
        Assert.Equal(0x89, result[0]);
        Assert.Equal((byte)'P', result[1]);
        Assert.Equal((byte)'N', result[2]);
        Assert.Equal((byte)'G', result[3]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GeneratePngQrCode_ThrowsArgumentNullException_WhenContentIsNullOrWhitespace(string invalidContent)
    {
        Assert.Throws<ArgumentNullException>(() => _sut.GeneratePngQrCode(invalidContent));
    }
}
