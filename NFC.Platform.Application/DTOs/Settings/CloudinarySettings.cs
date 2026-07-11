namespace NFC.Platform.Application.DTOs.Settings;

/// <summary>
/// Configuration settings for Cloudinary API integration.
/// </summary>
public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}
