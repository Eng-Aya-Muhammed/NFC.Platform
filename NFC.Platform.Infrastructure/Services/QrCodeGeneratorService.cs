using QRCoder;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.Infrastructure.Services;

/// <summary>
/// Generates QR code images as raw PNG bytes using the QRCoder library.
/// Registered as Singleton because QRCodeGenerator itself is stateless
/// and creating it once avoids repeated GC pressure.
/// </summary>
public sealed class QrCodeGeneratorService : IQrCodeGenerator
{
    /// <summary>
    /// Renders a QR code that encodes <paramref name="url"/> and returns the result as a PNG byte array.
    /// </summary>
    /// <param name="url">The URL to encode inside the QR code (e.g. https://onpoint-teasting.com/c/ABC123).</param>
    /// <param name="pixelSize">
    /// Size (in pixels) of each individual QR module.
    /// 10 px ≈ 250×250 px total — large enough for reliable scanning without bloating storage.
    /// </param>
    public byte[] GeneratePngBytes(string url, int pixelSize = 10)
    {
        using var generator = new QRCodeGenerator();
        // ECCLevel.M: 15 % error correction — good balance between density and scan robustness.
        var qrData = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(pixelSize);
    }
}
