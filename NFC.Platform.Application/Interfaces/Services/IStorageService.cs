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

    /// <summary>
    /// Uploads raw bytes (e.g. a programmatically generated QR code PNG) to Cloudinary.
    /// Avoids the overhead of wrapping bytes in an <see cref="Microsoft.AspNetCore.Http.IFormFile"/>.
    /// </summary>
    /// <param name="bytes">The raw file bytes to upload.</param>
    /// <param name="fileName">File name hint used by Cloudinary (e.g. "qr-ABC123.png").</param>
    /// <param name="folderName">Cloudinary sub-folder relative to the nfc-platform root.</param>
    Task<UploadResultDto> UploadBytesAsImageAsync(byte[] bytes, string fileName, string folderName);
}
