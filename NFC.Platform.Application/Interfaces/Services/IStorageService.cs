using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Service abstraction for uploading and deleting files from a cloud storage provider (like Cloudinary).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads an image file to the storage provider.
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <param name="folderName">The folder path/name in the storage container.</param>
    /// <returns>The secure URL of the uploaded image.</returns>
    Task<string> UploadImageAsync(IFormFile file, string folderName);

    /// <summary>
    /// Uploads a raw file (e.g., Excel sheets, CSVs) to the storage provider.
    /// </summary>
    /// <param name="file">The raw file to upload.</param>
    /// <param name="folderName">The folder path/name in the storage container.</param>
    /// <returns>The secure URL of the uploaded raw file.</returns>
    Task<string> UploadRawFileAsync(IFormFile file, string folderName);

    /// <summary>
    /// Deletes a file from the storage provider using its URL.
    /// </summary>
    /// <param name="fileUrl">The full secure URL of the file to delete.</param>
    /// <returns>True if deletion succeeded; otherwise false.</returns>
    Task<bool> DeleteFileAsync(string fileUrl);
}
