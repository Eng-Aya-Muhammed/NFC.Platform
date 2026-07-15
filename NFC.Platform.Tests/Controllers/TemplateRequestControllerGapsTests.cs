namespace NFC.Platform.Tests.Controllers
{
    public class TemplateRequestControllerGapsTests
    {
        private readonly ITemplateRequestService _templateRequestService;
        private readonly ICurrentTenant _currentTenant;
        private readonly TemplateRequestController _sut;

        public TemplateRequestControllerGapsTests()
        {
            _templateRequestService = Substitute.For<ITemplateRequestService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _sut = new TemplateRequestController(_templateRequestService, _currentTenant);
        }

        [Fact]
        public async Task GetRequestById_ShouldCallGetRequestByIdAsync_OnTemplateRequestService()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedResult = ServiceResult<TemplateRequestDto>.Success(new TemplateRequestDto());

            _templateRequestService.GetRequestByIdAsync(id).Returns(expectedResult);

            // Act
            var result = await _sut.GetRequestById(id) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _templateRequestService.Received(1).GetRequestByIdAsync(id);
        }
    }
}
