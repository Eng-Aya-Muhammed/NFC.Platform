using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.Settings;
using NFC.Platform.Application.DTOs.Upload;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NFC.Platform.API.Controllers
{
    /// <summary>
    /// API Controller for handling secure file and image uploads to Cloudinary.
    /// Returns both SecureUrl and PublicId for each uploaded file.
    /// </summary>
    [ApiController]
    [Route("api/uploads")]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly UploadSettings _uploadSettings;

        public UploadController(
            IStorageService storageService,
            IMessageService messageService,
            ICurrentTenant currentTenant,
            IOptions<UploadSettings>? uploadSettings = null)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _currentTenant  = currentTenant  ?? throw new ArgumentNullException(nameof(currentTenant));
            _uploadSettings = uploadSettings?.Value ?? new UploadSettings();
        }

        /// <summary>
        /// Uploads an image file to Cloudinary.
        /// Returns both the SecureUrl and PublicId so the client can store both.
        /// </summary>
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "general")
        {
            if (file == null || file.Length == 0)
                return BadRequest(_messageService.Get("NoFileUploaded"));

            if (file.Length > _uploadSettings.MaxImageSizeBytes)
                return BadRequest(_messageService.Get("FileTooLarge"));

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, extension) == -1)
                return BadRequest(_messageService.Get("InvalidImageExtension"));

            if (!FileValidationHelper.IsValidImageContentType(file.ContentType))
                return BadRequest(_messageService.Get("InvalidContentType"));

            if (!FileValidationHelper.IsValidImageSignature(file))
                return BadRequest(_messageService.Get("InvalidFileSignature"));

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
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(_messageService.Get("NoFileUploaded"));

            if (file.Length > _uploadSettings.MaxExcelSizeBytes)
                return BadRequest(_messageService.Get("FileTooLarge"));

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, extension) == -1)
                return BadRequest(_messageService.Get("InvalidExcelExtension"));

            if (!FileValidationHelper.IsValidExcelContentType(file.ContentType))
                return BadRequest(_messageService.Get("InvalidContentType"));

            if (!FileValidationHelper.IsValidExcelSignature(file))
                return BadRequest(_messageService.Get("InvalidFileSignature"));

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

        /// <summary>
        /// Uploads a PDF file to Cloudinary.
        /// Returns both the SecureUrl and PublicId.
        /// </summary>
        [HttpPost("pdf")]
        public async Task<IActionResult> UploadPdf(IFormFile file, [FromQuery] string folder = "documents")
        {
            if (file == null || file.Length == 0)
                return BadRequest(_messageService.Get("NoFileUploaded"));

            if (file.Length > _uploadSettings.MaxPdfSizeBytes)
                return BadRequest(_messageService.Get("FileTooLarge"));

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
                return BadRequest(_messageService.Get("InvalidPdfExtension"));

            if (!FileValidationHelper.IsValidPdfContentType(file.ContentType))
                return BadRequest(_messageService.Get("InvalidContentType"));

            if (!FileValidationHelper.IsValidPdfSignature(file))
                return BadRequest(_messageService.Get("InvalidFileSignature"));

            try
            {
                var tenantId = _currentTenant.TenantId?.ToString() ?? "no-tenant";
                var userId = _currentTenant.UserId?.ToString() ?? "no-user";
                var folderPath = $"{tenantId}/{userId}/{folder.Trim('/')}";

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
