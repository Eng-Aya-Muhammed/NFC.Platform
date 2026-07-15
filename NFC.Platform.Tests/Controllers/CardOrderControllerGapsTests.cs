namespace NFC.Platform.Tests.Controllers
{
    public class CardOrderControllerGapsTests
    {
        private readonly ICardOrderService _cardOrderService;
        private readonly CardOrderController _sut;

        public CardOrderControllerGapsTests()
        {
            _cardOrderService = Substitute.For<ICardOrderService>();
            _sut = new CardOrderController(_cardOrderService);
        }

        [Fact]
        public async Task GetPricing_ShouldCallGetOrderPricingAsync_OnCardOrderService()
        {
            // Arrange
            var cardType = "plastic";
            var quantity = 5;
            var expectedResult = ServiceResult<OrderPricingResponseDto>.Success(new OrderPricingResponseDto());

            _cardOrderService.GetOrderPricingAsync(cardType, quantity).Returns(expectedResult);

            // Act
            var result = await _sut.GetPricing(cardType, quantity) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetOrderPricingAsync(cardType, quantity);
        }

        [Fact]
        public async Task GetEmployeesImportStatus_ShouldCallGetEmployeesImportStatusAsync_OnCardOrderService()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var expectedResult = ServiceResult<EmployeesImportStatusDto>.Success(new EmployeesImportStatusDto());

            _cardOrderService.GetEmployeesImportStatusAsync(orderId).Returns(expectedResult);

            // Act
            var result = await _sut.GetEmployeesImportStatus(orderId) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetEmployeesImportStatusAsync(orderId);
        }
    }
}
