using Microsoft.AspNetCore.Http;
using NFC.Platform.Application.DTOs.Upload;
using NFC.Platform.BuildingBlocks.Localization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NFC.Platform.API.Controllers
{
    /// <summary>
    /// API Controller for handling file and image uploads to Cloudinary.
    /// Returns both SecureUrl and PublicId for each uploaded file.
    /// </summary>
    [ApiController]
    [Route("api/uploads")]
    [Authorize]
    public class UploadController(IStorageService storageService, IMessageService messageService, ICurrentTenant currentTenant) : ControllerBase
    {
        private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        /// <summary>
        /// Uploads an image file to Cloudinary.
        /// Returns both the SecureUrl and PublicId so the client can store both.
        /// </summary>
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromQuery] string folder = "general")
        {
            if (file == null || file.Length == 0)
                return BadRequest(_messageService.Get("NoFileUploaded"));

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (Array.IndexOf(allowedExtensions, extension) == -1)
                return BadRequest(_messageService.Get("InvalidImageExtension"));

            try
            {
                var tenantId = _currentTenant.TenantId?.ToString() ?? "no-tenant";
                var userId = _currentTenant.UserId?.ToString() ?? "no-user";
                var folderPath = $"{tenantId}/{userId}/{folder.Trim('/')}";

                var result = await _storageService.UploadImageAsync(file, folderPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, _messageService.Get("UploadError", ex.Message));
            }
        }

        /// <summary>
        /// Uploads an Excel file to Cloudinary.
        /// Returns both the SecureUrl and PublicId.
        /// </summary>
        [HttpPost("excel")]
        public async Task<IActionResult> UploadExcel([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(_messageService.Get("NoFileUploaded"));

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (Array.IndexOf(allowedExtensions, extension) == -1)
                return BadRequest(_messageService.Get("InvalidExcelExtension"));

            try
            {
                var tenantId = _currentTenant.TenantId?.ToString() ?? "no-tenant";
                var userId = _currentTenant.UserId?.ToString() ?? "no-user";
                var folderPath = $"{tenantId}/{userId}/excel-orders";

                var result = await _storageService.UploadRawFileAsync(file, folderPath);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, _messageService.Get("UploadError", ex.Message));
            }
        }
    }
}
