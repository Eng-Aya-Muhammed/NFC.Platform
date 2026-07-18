namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Generates QR code images as raw PNG bytes.
/// Implementation lives in the Infrastructure layer (QRCoder).
/// Keeping the contract here keeps the Application layer free of QRCoder dependencies.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>
    /// Renders a QR code that encodes <paramref name="url"/> and returns it as a PNG byte array.
    /// </summary>
    /// <param name="url">The URL to encode inside the QR code.</param>
    /// <param name="pixelSize">Size (in pixels) of each individual QR module. Default: 10.</param>
    byte[] GeneratePngBytes(string url, int pixelSize = 10);
}
