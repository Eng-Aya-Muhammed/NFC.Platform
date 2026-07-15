using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs.Upload;

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

            // Mock current tenant and user IDs
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _currentTenant.UserId.Returns(Guid.NewGuid());

            // Mock translations to make tests match expected responses
            _messageService.Get("NoFileUploaded").Returns("No file was uploaded.");
            _messageService.Get("InvalidImageExtension").Returns("Only image files (.jpg, .jpeg, .png, .webp, .gif) are allowed.");
            _messageService.Get("InvalidExcelExtension").Returns("Only Excel files (.xls, .xlsx) are allowed.");
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
            // Act
            var result = await _sut.UploadImage(null!) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("No file was uploaded.", result.Value);
        }

        [Fact]
        public async Task UploadImage_ReturnsBadRequest_WhenFileLengthIsZero()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(0);

            // Act
            var result = await _sut.UploadImage(file) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("No file was uploaded.", result.Value);
        }

        [Fact]
        public async Task UploadImage_ReturnsBadRequest_WhenExtensionNotAllowed()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("document.pdf");

            // Act
            var result = await _sut.UploadImage(file) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Only image files (.jpg, .jpeg, .png, .webp, .gif) are allowed.", result.Value);
        }

        [Fact]
        public async Task UploadImage_ReturnsOk_WhenUploadSucceeds()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("photo.jpg");
            var expectedUrl = "https://res.cloudinary.com/demo/image/upload/photo.jpg";

            _storageService.UploadImageAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            // Act
            var result = await _sut.UploadImage(file, "profile-pics") as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            
            // Check that the returned value contains the Url
            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadExcel_ReturnsBadRequest_WhenFileIsNull()
        {
            // Act
            var result = await _sut.UploadExcel(null!) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("No file was uploaded.", result.Value);
        }

        [Fact]
        public async Task UploadExcel_ReturnsBadRequest_WhenExtensionNotAllowed()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("image.png");

            // Act
            var result = await _sut.UploadExcel(file) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Only Excel files (.xls, .xlsx) are allowed.", result.Value);
        }

        [Fact]
        public async Task UploadExcel_ReturnsOk_WhenUploadSucceeds()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("employees.xlsx");
            var expectedUrl = "https://res.cloudinary.com/demo/raw/upload/employees.xlsx";

            _storageService.UploadRawFileAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            // Act
            var result = await _sut.UploadExcel(file) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            
            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadImage_ReturnsOk_WhenExtensionIsUpperCase()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("PHOTO.PNG");
            var expectedUrl = "https://res.cloudinary.com/demo/image/upload/photo.png";

            _storageService.UploadImageAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            // Act
            var result = await _sut.UploadImage(file) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            
            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadExcel_ReturnsOk_WhenExtensionIsUpperCase()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("EMPLOYEES.XLSX");
            var expectedUrl = "https://res.cloudinary.com/demo/raw/upload/employees.xlsx";

            _storageService.UploadRawFileAsync(file, Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto { SecureUrl = expectedUrl }));

            // Act
            var result = await _sut.UploadExcel(file) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            
            var value = result.Value as UploadResultDto;
            Assert.NotNull(value);
            Assert.Equal(expectedUrl, value.SecureUrl);
        }

        [Fact]
        public async Task UploadImage_ReturnsInternalServerError_WhenStorageServiceThrows()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("photo.jpg");
            var exceptionMessage = "Cloud connection timeout.";

            _storageService.UploadImageAsync(file, Arg.Any<string>())
                .Returns(Task.FromException<UploadResultDto>(new Exception(exceptionMessage)));

            // Act
            var result = await _sut.UploadImage(file) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Contains(exceptionMessage, result.Value?.ToString());
        }

        [Fact]
        public async Task UploadExcel_ReturnsInternalServerError_WhenStorageServiceThrows()
        {
            // Arrange
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(100);
            file.FileName.Returns("employees.xlsx");
            var exceptionMessage = "Cloud connection timeout.";

            _storageService.UploadRawFileAsync(file, Arg.Any<string>())
                .Returns(Task.FromException<UploadResultDto>(new Exception(exceptionMessage)));

            // Act
            var result = await _sut.UploadExcel(file) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            Assert.Contains(exceptionMessage, result.Value?.ToString());
        }

        [Fact]
        public void UploadController_ShouldHaveAuthorizeAttribute()
        {
            // Arrange & Act
            var type = typeof(UploadController);
            var attributes = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void UploadController_ShouldHaveApiControllerAttribute()
        {
            // Arrange & Act
            var type = typeof(UploadController);
            var attributes = type.GetCustomAttributes(typeof(ApiControllerAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void UploadController_ShouldHaveRouteAttributeWithCorrectTemplate()
        {
            // Arrange & Act
            var type = typeof(UploadController);
            var attributes = type.GetCustomAttributes(typeof(RouteAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
            var routeAttribute = attributes[0] as RouteAttribute;
            Assert.NotNull(routeAttribute);
            Assert.Equal("api/uploads", routeAttribute.Template);
        }
    }
}
