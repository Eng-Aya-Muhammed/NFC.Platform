using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace NFC.Platform.Infrastructure.Services;

/// <summary>
/// Infrastructure service implementation for Cloudinary file uploads.
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

    public async Task<string> UploadImageAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
        {
            return string.Empty;
        }

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"nfc-platform/{folderName.Trim('/')}",
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary Image Upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl?.ToString() ?? string.Empty;
    }

    public async Task<string> UploadRawFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
        {
            return string.Empty;
        }

        using var stream = file.OpenReadStream();
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"nfc-platform/{folderName.Trim('/')}"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary Raw File Upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl?.ToString() ?? string.Empty;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return false;
        }

        try
        {
            var (publicId, resourceType) = ParseCloudinaryUrl(fileUrl);
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType
            };

            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);
            return deletionResult.Result == "ok";
        }
        catch
        {
            // Log deletion failure or return false
            return false;
        }
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
        {
            throw new ArgumentException("Invalid Cloudinary URL format.");
        }

        // Determine resource type from the segment preceding "upload" (e.g. image, raw, video)
        string typeStr = segments[uploadIndex - 1].Trim('/');
        var resourceType = ResourceType.Image;
        if (typeStr.Equals("raw", StringComparison.OrdinalIgnoreCase))
        {
            resourceType = ResourceType.Raw;
        }
        else if (typeStr.Equals("video", StringComparison.OrdinalIgnoreCase))
        {
            resourceType = ResourceType.Video;
        }

        // Identify starting segment of the public ID path
        int startIndex = uploadIndex + 1;
        if (startIndex < segments.Length)
        {
            string nextSeg = segments[startIndex].Trim('/');
            // Skip the version segment (e.g., "v1571218039")
            if (nextSeg.StartsWith('v') && long.TryParse(nextSeg.AsSpan(1), out _))
            {
                startIndex++;
            }
        }

        // Join the remaining segments to form the full folder path and public ID
        var remainingSegments = new List<string>();
        for (int i = startIndex; i < segments.Length; i++)
        {
            remainingSegments.Add(Uri.UnescapeDataString(segments[i].Trim('/')));
        }

        string publicIdWithPath = string.Join("/", remainingSegments);

        // Images/videos strip the file extension, whereas raw resources retain it in Cloudinary
        if (resourceType != ResourceType.Raw)
        {
            int lastDotIndex = publicIdWithPath.LastIndexOf('.');
            if (lastDotIndex != -1)
            {
                publicIdWithPath = publicIdWithPath[..lastDotIndex];
            }
        }

        return (publicIdWithPath, resourceType);
    }
}
