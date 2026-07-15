using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using NFC.Platform.Application.DTOs.Upload;

namespace NFC.Platform.Infrastructure.Services;

/// <summary>
/// Infrastructure service implementation for Cloudinary file uploads.
/// Returns both SecureUrl and PublicId so callers can later delete or replace assets.
/// </summary>
public class CloudinaryService : IStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var settings = config.Value;
        if (string.IsNullOrWhiteSpace(settings.CloudName) ||
            string.IsNullOrWhiteSpace(settings.ApiKey) ||
            string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            throw new ArgumentException("Cloudinary configuration values cannot be null or empty.");
        }

        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<UploadResultDto> UploadImageAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
            return new UploadResultDto();

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"nfc-platform/{folderName.Trim('/')}",
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new Exception($"Cloudinary Image Upload failed: {uploadResult.Error.Message}");

        return new UploadResultDto
        {
            SecureUrl = uploadResult.SecureUrl?.ToString() ?? string.Empty,
            PublicId = uploadResult.PublicId ?? string.Empty,
            ResourceType = "image",
            Format = uploadResult.Format ?? string.Empty,
            Bytes = uploadResult.Bytes
        };
    }

    public async Task<UploadResultDto> UploadRawFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
            return new UploadResultDto();

        using var stream = file.OpenReadStream();
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"nfc-platform/{folderName.Trim('/')}"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new Exception($"Cloudinary Raw File Upload failed: {uploadResult.Error.Message}");

        return new UploadResultDto
        {
            SecureUrl = uploadResult.SecureUrl?.ToString() ?? string.Empty,
            PublicId = uploadResult.PublicId ?? string.Empty,
            ResourceType = "raw",
            Format = uploadResult.Format ?? string.Empty,
            Bytes = uploadResult.Bytes
        };
    }

    public async Task<bool> DeleteFileByPublicIdAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return false;

        try
        {
            var deletionResult = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            return deletionResult.Result == "ok";
        }
        catch { return false; }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return false;

        try
        {
            var (publicId, resourceType) = ParseCloudinaryUrl(fileUrl);
            var deletionParams = new DeletionParams(publicId) { ResourceType = resourceType };
            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);
            return deletionResult.Result == "ok";
        }
        catch { return false; }
    }

    /// <summary>
    /// Parses a Cloudinary URL to extract the public ID and the resource type.
    /// </summary>
    private static (string PublicId, ResourceType ResourceType) ParseCloudinaryUrl(string fileUrl)
    {
        var uri = new Uri(fileUrl);
        var segments = uri.Segments;

        int uploadIndex = -1;
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i].Trim('/') == "upload")
            {
                uploadIndex = i;
                break;
            }
        }

        if (uploadIndex == -1 || uploadIndex >= segments.Length - 1)
            throw new ArgumentException("Invalid Cloudinary URL format.");

        string typeStr = segments[uploadIndex - 1].Trim('/');
        var resourceType = ResourceType.Image;
        if (typeStr.Equals("raw", StringComparison.OrdinalIgnoreCase))
            resourceType = ResourceType.Raw;
        else if (typeStr.Equals("video", StringComparison.OrdinalIgnoreCase))
            resourceType = ResourceType.Video;

        int startIndex = uploadIndex + 1;
        if (startIndex < segments.Length)
        {
            string nextSeg = segments[startIndex].Trim('/');
            if (nextSeg.StartsWith('v') && long.TryParse(nextSeg.AsSpan(1), out _))
                startIndex++;
        }

        var remainingSegments = new List<string>();
        for (int i = startIndex; i < segments.Length; i++)
            remainingSegments.Add(Uri.UnescapeDataString(segments[i].Trim('/')));

        string publicIdWithPath = string.Join("/", remainingSegments);

        if (resourceType != ResourceType.Raw)
        {
            int lastDotIndex = publicIdWithPath.LastIndexOf('.');
            if (lastDotIndex != -1)
                publicIdWithPath = publicIdWithPath[..lastDotIndex];
        }

        return (publicIdWithPath, resourceType);
    }
}
