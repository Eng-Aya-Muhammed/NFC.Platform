using Microsoft.AspNetCore.Http;
using NFC.Platform.Application.DTOs.Upload;
using System.Threading.Tasks;

namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Service abstraction for uploading and deleting files from a cloud storage provider (Cloudinary).
/// Returns both secure_url and public_id so callers can later delete or replace assets.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads an image file. Returns secure_url + public_id from Cloudinary.
    /// </summary>
    Task<UploadResultDto> UploadImageAsync(IFormFile file, string folderName);

    /// <summary>
    /// Uploads a raw file (Excel, PDF, AI). Returns secure_url + public_id from Cloudinary.
    /// </summary>
    Task<UploadResultDto> UploadRawFileAsync(IFormFile file, string folderName);

    /// <summary>
    /// Deletes a file from Cloudinary using its public_id.
    /// </summary>
    Task<bool> DeleteFileByPublicIdAsync(string publicId);

    /// <summary>
    /// Deletes a file from Cloudinary using its secure URL (extracts public_id internally).
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl);
}
