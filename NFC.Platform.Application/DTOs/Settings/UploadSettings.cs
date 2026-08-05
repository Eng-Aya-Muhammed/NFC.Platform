namespace NFC.Platform.Application.DTOs.Settings;

public class UploadSettings
{
    public long MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024;

    public long MaxExcelSizeBytes { get; set; } = 50 * 1024 * 1024;

    public long MaxPdfSizeBytes { get; set; } = 50 * 1024 * 1024;
}
