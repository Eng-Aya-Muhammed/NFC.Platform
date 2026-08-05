using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs.Upload;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers
{
    public class UploadControllerTests
    {
        private readonly IStorageService _storageService;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly UploadController _sut;

        public UploadControllerTests()
        {
            _storageService = Substitute.For<IStorageService>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _currentTenant.UserId.Returns(Guid.NewGuid());

            _messageService.Get("NoFileUploaded").Returns("No file was uploaded.");
            _messageService.Get("InvalidImageExtension").Returns("Only image files (.jpg, .jpeg, .png, .webp, .gif) are allowed.");
            _messageService.Get("InvalidExcelExtension").Returns("Only Excel files (.xls, .xlsx) are allowed.");
            _messageService.Get("InvalidPdfExtension").Returns("Only PDF files (.pdf) are allowed.");
            _messageService.Get("InvalidContentType").Returns("The file content type is not supported.");
            _messageService.Get("FileTooLarge").Returns("File size exceeds the maximum allowed limit.");
            _messageService.Get("InvalidFileSignature").Returns("File content does not match the allowed file signature.");
            _messageService.Get("UploadError", Arg.Any<object[]>()).Returns(x =>
            {
                var args = x.Arg<object[]>();
                return $"An error occurred during upload: {args[0]}";
            });

            _sut = new UploadController(_storageService, _messageService, _currentTenant);
        }

        [Fact]
        public async Task UploadImage_ReturnsBadRequest_WhenFileIsNull()
        {
            var result = await _sut.UploadImage(null!) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("No file was uploaded.", result.Value);
        }

        [Fact]
        public async Task UploadImage_ReturnsBadRequest_WhenFileLengthIsZero()
        {
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(0);

            var result = await _sut.UploadImage(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("No file was uploaded.", result.Value);
        }

        [Fact]
        public async Task UploadImage_ReturnsBadRequest_WhenExtensionNotAllowed()
        {
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("document.pdf");

            var result = await _sut.UploadImage(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Only image files (.jpg, .jpeg, .png, .webp, .gif) are allowed.", result.Value);
        }

        [Fact]
        public async Task UploadImage_ReturnsBadRequest_WhenFileExceedsMaxSize()
        {
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(11 * 1024 * 1024);
            file.FileName.Returns("photo.jpg");

            var result = await _sut.UploadImage(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("File size exceeds the maximum allowed limit.", result.Value);
        }

        [Fact]
        public async Task UploadImage_ReturnsBadRequest_WhenMagicBytesDoNotMatch()
        {
            var pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var file = CreateMockFile("fake.jpg", pdfHeader, contentType: "image/jpeg");

            var result = await _sut.UploadImage(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("File content does not match the allowed file signature.", result.Value);
        }

        [Fact]
        public async Task UploadImage_ReturnsOk_WhenUploadSucceeds()
        {
            var jpgHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
            var file = CreateMockFile("photo.jpg", jpgHeader, contentType: "image/jpeg");
            var expectedUrl = "https://res.cloudinary.com/demo/image/upload/photo.jpg";

            _storageService.UploadImageAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            var result = await _sut.UploadImage(file, "profile-pics") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadExcel_ReturnsBadRequest_WhenFileIsNull()
        {
            var result = await _sut.UploadExcel(null!) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("No file was uploaded.", result.Value);
        }

        [Fact]
        public async Task UploadExcel_ReturnsBadRequest_WhenFileExceedsMaxSize()
        {
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(51 * 1024 * 1024);
            file.FileName.Returns("employees.xlsx");

            var result = await _sut.UploadExcel(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("File size exceeds the maximum allowed limit.", result.Value);
        }

        [Fact]
        public async Task UploadExcel_ReturnsBadRequest_WhenMagicBytesDoNotMatch()
        {
            var exeHeader = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
            var file = CreateMockFile("employees.xlsx", exeHeader, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var result = await _sut.UploadExcel(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("File content does not match the allowed file signature.", result.Value);
        }

        [Fact]
        public async Task UploadExcel_ReturnsBadRequest_WhenExtensionNotAllowed()
        {
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("image.png");

            var result = await _sut.UploadExcel(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Only Excel files (.xls, .xlsx) are allowed.", result.Value);
        }

        [Fact]
        public async Task UploadExcel_ReturnsOk_WhenUploadSucceeds()
        {
            var xlsxHeader = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 };
            var file = CreateMockFile("employees.xlsx", xlsxHeader, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            var expectedUrl = "https://res.cloudinary.com/demo/raw/upload/employees.xlsx";

            _storageService.UploadRawFileAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            var result = await _sut.UploadExcel(file) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadPdf_ReturnsBadRequest_WhenFileExceedsMaxSize()
        {
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(51 * 1024 * 1024);
            file.FileName.Returns("report.pdf");

            var result = await _sut.UploadPdf(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("File size exceeds the maximum allowed limit.", result.Value);
        }

        [Fact]
        public async Task UploadPdf_ReturnsBadRequest_WhenMagicBytesDoNotMatch()
        {
            var exeHeader = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
            var file = CreateMockFile("report.pdf", exeHeader, contentType: "application/pdf");

            var result = await _sut.UploadPdf(file) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("File content does not match the allowed file signature.", result.Value);
        }

        [Fact]
        public async Task UploadPdf_ReturnsOk_WhenUploadSucceeds()
        {
            var pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x35 };
            var file = CreateMockFile("report.pdf", pdfHeader, contentType: "application/pdf");
            var expectedUrl = "https://res.cloudinary.com/demo/raw/upload/report.pdf";

            _storageService.UploadRawFileAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            var result = await _sut.UploadPdf(file) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadImage_ReturnsOk_WhenExtensionIsUpperCase()
        {
            var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var file = CreateMockFile("PHOTO.PNG", pngHeader, contentType: "image/png");
            var expectedUrl = "https://res.cloudinary.com/demo/image/upload/photo.png";

            _storageService.UploadImageAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            var result = await _sut.UploadImage(file) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadExcel_ReturnsOk_WhenExtensionIsUpperCase()
        {
            var xlsxHeader = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 };
            var file = CreateMockFile("EMPLOYEES.XLSX", xlsxHeader, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            var expectedUrl = "https://res.cloudinary.com/demo/raw/upload/employees.xlsx";

            _storageService.UploadRawFileAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            var result = await _sut.UploadExcel(file) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadImage_ReturnsInternalServerError_WhenStorageServiceThrows()
        {
            var jpgHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
            var file = CreateMockFile("photo.jpg", jpgHeader, contentType: "image/jpeg");
            var exceptionMessage = "Cloud connection timeout.";

            _storageService.UploadImageAsync(file, Arg.Any<string>())
                .Returns(Task.FromException<UploadResultDto>(new Exception(exceptionMessage)));

            var result = await _sut.UploadImage(file) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Contains(exceptionMessage, result.Value?.ToString());
        }

        [Fact]
        public async Task UploadExcel_ReturnsInternalServerError_WhenStorageServiceThrows()
        {
            var xlsxHeader = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 };
            var file = CreateMockFile("employees.xlsx", xlsxHeader, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            var exceptionMessage = "Cloud connection timeout.";

            _storageService.UploadRawFileAsync(file, Arg.Any<string>())
                .Returns(Task.FromException<UploadResultDto>(new Exception(exceptionMessage)));

            var result = await _sut.UploadExcel(file) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Contains(exceptionMessage, result.Value?.ToString());
        }

        private static IFormFile CreateMockFile(string fileName, byte[] headerBytes, long length = 100, string? contentType = null)
        {
            var file = Substitute.For<IFormFile>();
            file.FileName.Returns(fileName);
            file.Length.Returns(length);
            file.ContentType.Returns(contentType ?? "");
            file.OpenReadStream().Returns(_ => new MemoryStream(headerBytes));
            return file;
        }

        [Fact]
        public void UploadController_ShouldHaveAuthorizeAttribute()
        {
            var type = typeof(UploadController);
            var attributes = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);

            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void UploadController_ShouldHaveApiControllerAttribute()
        {
            var type = typeof(UploadController);
            var attributes = type.GetCustomAttributes(typeof(ApiControllerAttribute), true);

            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void UploadController_ShouldHaveRouteAttributeWithCorrectTemplate()
        {
            var type = typeof(UploadController);
            var attributes = type.GetCustomAttributes(typeof(RouteAttribute), true);

            Assert.NotEmpty(attributes);
            var routeAttribute = attributes[0] as RouteAttribute;
            Assert.NotNull(routeAttribute);
            Assert.Equal("api/uploads", routeAttribute.Template);
        }
    }
}
