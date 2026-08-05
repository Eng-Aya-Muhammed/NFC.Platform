namespace NFC.Platform.Application.Interfaces.Services;

public interface IQrCodeService
{
    byte[] GeneratePngQrCode(string content, int pixelsPerModule = 20);
}
