using System;
using NFC.Platform.Application.Interfaces.Services;
using QRCoder;

namespace NFC.Platform.Infrastructure.Services;

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
