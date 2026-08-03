namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Service contract for generating QR code images.
/// Pure C# PNG generation without GDI+/System.Drawing dependencies.
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// Generates a PNG byte array for the provided payload string.
    /// </summary>
    /// <param name="content">The payload string (URL, vCard, text) encoded inside the QR code.</param>
    /// <param name="pixelsPerModule">Size multiplier for the generated image (default 20 = ~400x400px image).</param>
    /// <returns>PNG image byte array.</returns>
    byte[] GeneratePngQrCode(string content, int pixelsPerModule = 20);
}
