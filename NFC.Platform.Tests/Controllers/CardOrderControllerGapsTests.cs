using NFC.Platform.API.Models;

namespace NFC.Platform.Tests.Controllers
{
    public class CardOrderControllerGapsTests
    {
        private readonly ICardOrderService _cardOrderService;
        private readonly IMessageService _messageService;
        private readonly CardOrderController _sut;

        public CardOrderControllerGapsTests()
        {
            _cardOrderService = Substitute.For<ICardOrderService>();
            _messageService = Substitute.For<IMessageService>();
            _sut = new CardOrderController(_cardOrderService, _messageService);
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

        [Fact]
        public async Task PlaceBulkOrderFromExcel_ShouldCallQueueEmployeeImportJobAsync_OnCardOrderService()
        {
            // Arrange
            var file = Substitute.For<Microsoft.AspNetCore.Http.IFormFile>();
            var fileStream = new System.IO.MemoryStream();
            file.OpenReadStream().Returns(fileStream);

            var request = new ImportEmployeesAndOrderCardsRequest
            {
                File = file,
                CardType = CardType.Plastic,
                CardDesignType = CardDesignType.BuiltInTemplate
            };

            var expectedResult = ServiceResult<EmployeeImportJob>.Success(new EmployeeImportJob());
            _cardOrderService.QueueEmployeeImportJobAsync(
                Arg.Any<Microsoft.AspNetCore.Http.IFormFile>(),
                Arg.Any<CardType>(),
                Arg.Any<CardDesignType>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>()).Returns(expectedResult);

            // Act
            var result = await _sut.PlaceBulkOrderFromExcel(request) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(202, result.StatusCode);
            await _cardOrderService.Received(1).QueueEmployeeImportJobAsync(
                file,
                CardType.Plastic,
                CardDesignType.BuiltInTemplate,
                Arg.Any<Guid?>(),
                Arg.Any<string?>()
            );
        }
    }
}
