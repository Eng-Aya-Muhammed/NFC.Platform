namespace NFC.Platform.Application.DTOs.Upload;

/// <summary>
/// Response returned from any file upload operation.
/// Holds both the displayable URL and Cloudinary's internal identifier
/// needed later to replace or delete the asset.
/// </summary>
public class UploadResultDto
{
    public string SecureUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long Bytes { get; set; }
}
