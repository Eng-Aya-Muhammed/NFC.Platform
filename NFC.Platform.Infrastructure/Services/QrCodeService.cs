using System;
using QRCoder;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.Infrastructure.Services;

/// <summary>
/// Infrastructure service for generating QR code PNG byte arrays using QRCoder.
/// Uses PngByteQRCode for pure C# cross-platform performance (no GDI+/System.Drawing dependencies).
/// </summary>
public class QrCodeService : IQrCodeService
{
    public byte[] GeneratePngQrCode(string content, int pixelsPerModule = 20)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentNullException(nameof(content));

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelsPerModule);
    }
}
