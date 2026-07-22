namespace NFC.Platform.Tests.Services
{
    public class CloudinaryServiceTests
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenConfigIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CloudinaryService(null!));
        }

        [Theory]
        [InlineData("", "key", "secret")]
        [InlineData("cloud", "", "secret")]
        [InlineData("cloud", "key", "")]
        [InlineData(null, "key", "secret")]
        [InlineData("cloud", null, "secret")]
        [InlineData("cloud", "key", null)]
        public void Constructor_ThrowsArgumentException_WhenSettingsAreInvalid(string? cloudName, string? apiKey, string? apiSecret)
        {
            // Arrange
            var options = Options.Create(new CloudinarySettings
            {
                CloudName = cloudName!,
                ApiKey = apiKey!,
                ApiSecret = apiSecret!
            });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new CloudinaryService(options));
        }

        [Fact]
        public async Task UploadImageAsync_ReturnsEmptyString_WhenFileIsNull()
        {
            // Arrange
            var options = Options.Create(new CloudinarySettings
            {
                CloudName = "test-cloud",
                ApiKey = "test-key",
                ApiSecret = "test-secret"
            });
            var service = new CloudinaryService(options);

            // Act
            var result = await service.UploadImageAsync(null!, "pics");

            // Assert
            Assert.Equal(string.Empty, result.SecureUrl);
        }

        [Fact]
        public async Task UploadImageAsync_ReturnsEmptyString_WhenFileIsEmpty()
        {
            // Arrange
            var options = Options.Create(new CloudinarySettings
            {
                CloudName = "test-cloud",
                ApiKey = "test-key",
                ApiSecret = "test-secret"
            });
            var service = new CloudinaryService(options);
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(0);

            // Act
            var result = await service.UploadImageAsync(file, "pics");

            // Assert
            Assert.Equal(string.Empty, result.SecureUrl);
        }

        [Fact]
        public async Task UploadRawFileAsync_ReturnsEmptyString_WhenFileIsNull()
        {
            // Arrange
            var options = Options.Create(new CloudinarySettings
            {
                CloudName = "test-cloud",
                ApiKey = "test-key",
                ApiSecret = "test-secret"
            });
            var service = new CloudinaryService(options);

            // Act
            var result = await service.UploadRawFileAsync(null!, "excel");

            // Assert
            Assert.Equal(string.Empty, result.SecureUrl);
        }

        [Fact]
        public async Task UploadRawFileAsync_ReturnsEmptyString_WhenFileIsEmpty()
        {
            // Arrange
            var options = Options.Create(new CloudinarySettings
            {
                CloudName = "test-cloud",
                ApiKey = "test-key",
                ApiSecret = "test-secret"
            });
            var service = new CloudinaryService(options);
            var file = Substitute.For<IFormFile>();
            file.Length.Returns(0);

            // Act
            var result = await service.UploadRawFileAsync(file, "excel");

            // Assert
            Assert.Equal(string.Empty, result.SecureUrl);
        }

        [Fact]
        public async Task DeleteFileAsync_ReturnsFalse_WhenUrlIsEmpty()
        {
            // Arrange
            var options = Options.Create(new CloudinarySettings
            {
                CloudName = "test-cloud",
                ApiKey = "test-key",
                ApiSecret = "test-secret"
            });
            var service = new CloudinaryService(options);

            // Act
            var result = await service.DeleteFileAsync(string.Empty);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFileAsync_ReturnsFalse_WhenUrlIsWhitespace()
        {
            // Arrange
            var options = Options.Create(new CloudinarySettings
            {
                CloudName = "test-cloud",
                ApiKey = "test-key",
                ApiSecret = "test-secret"
            });
            var service = new CloudinaryService(options);

            // Act
            var result = await service.DeleteFileAsync("   ");

            // Assert
            Assert.False(result);
        }
    }
}
