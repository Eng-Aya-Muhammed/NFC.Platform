namespace NFC.Platform.Application.DTOs.Settings;

public class UploadSettings
{
    /// <summary>
    /// Maximum allowed file size for image uploads in bytes (Default: 10 MB).
    /// </summary>
    public long MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Maximum allowed file size for Excel file uploads in bytes (Default: 50 MB).
    /// </summary>
    public long MaxExcelSizeBytes { get; set; } = 50 * 1024 * 1024; // 50 MB

    /// <summary>
    /// Maximum allowed file size for PDF file uploads in bytes (Default: 50 MB).
    /// </summary>
    public long MaxPdfSizeBytes { get; set; } = 50 * 1024 * 1024; // 50 MB
}
