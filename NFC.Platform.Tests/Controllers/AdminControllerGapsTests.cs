namespace NFC.Platform.Tests.Controllers
{
    public class AdminControllerGapsTests
    {
        private readonly IAdminService _adminService;
        private readonly ICardService _cardService;
        private readonly AdminController _sut;

        public AdminControllerGapsTests()
        {
            _adminService = Substitute.For<IAdminService>();
            _cardService = Substitute.For<ICardService>();
            _sut = new AdminController(_adminService, _cardService);
        }

        [Fact]
        public async Task GetOrdersPaged_ShouldPassCompanyIdToService()
        {
            // Arrange
            var request = new PaginationRequest();
            var status = OrderStatus.InPrinting;
            var companyId = Guid.NewGuid();
            var expectedResult = ServiceResult<PagedResult<AdminOrderSummaryDto>>.Success(
                PagedResult<AdminOrderSummaryDto>.Create(new List<AdminOrderSummaryDto>(), 0, 1, 10));

            _adminService.GetOrdersPagedAsync(request, status, companyId).Returns(expectedResult);

            // Act
            var result = await _sut.GetOrdersPaged(request, status, companyId) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _adminService.Received(1).GetOrdersPagedAsync(request, status, companyId);
        }

        [Fact]
        public async Task GetTemplateRequestsPaged_ShouldPassStatusToService()
        {
            // Arrange
            var request = new PaginationRequest();
            var status = TemplateRequestStatus.Completed;
            var expectedResult = ServiceResult<PagedResult<TemplateRequestDto>>.Success(
                PagedResult<TemplateRequestDto>.Create(new List<TemplateRequestDto>(), 0, 1, 10));

            _adminService.GetTemplateRequestsPagedAsync(request, status).Returns(expectedResult);

            // Act
            var result = await _sut.GetTemplateRequestsPaged(request, status) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _adminService.Received(1).GetTemplateRequestsPagedAsync(request, status);
        }

        [Fact]
        public async Task GetCards_ShouldCallGetCardsForEncodingAsync_OnCardService()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var status = "unassigned_code";
            var expectedResult = ServiceResult<List<CardDto>>.Success(new List<CardDto>());

            _cardService.GetCardsForEncodingAsync(orderId, status).Returns(expectedResult);

            // Act
            var result = await _sut.GetCards(orderId, status) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).GetCardsForEncodingAsync(orderId, status);
        }
    }
}
