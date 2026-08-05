using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NFC.Platform.Application.DTOs.Upload;

namespace NFC.Platform.Application.Interfaces.Services;

public interface IStorageService
{
    Task<UploadResultDto> UploadImageAsync(IFormFile file, string folderName);

    Task<UploadResultDto> UploadRawFileAsync(IFormFile file, string folderName);

    Task<bool> DeleteFileByPublicIdAsync(string publicId);

    Task<bool> DeleteFileAsync(string fileUrl);

    Task<UploadResultDto> UploadBytesAsImageAsync(byte[] bytes, string fileName, string folderName);
}
