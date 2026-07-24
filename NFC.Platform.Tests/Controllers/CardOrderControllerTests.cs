namespace NFC.Platform.Tests.Controllers
{
    public class CardOrderControllerTests
    {
        private readonly ICardOrderService _cardOrderService;
        private readonly ICardPricingService _cardPricingService;
        private readonly IEmployeeImportService _employeeImportService;
        private readonly IMessageService _messageService;
        private readonly CardOrderController _sut;

        public CardOrderControllerTests()
        {
            _cardOrderService = Substitute.For<ICardOrderService>();
            _cardPricingService = Substitute.For<ICardPricingService>();
            _employeeImportService = Substitute.For<IEmployeeImportService>();
            _messageService = Substitute.For<IMessageService>();
            _sut = new CardOrderController(_cardOrderService, _cardPricingService, _employeeImportService, _messageService);
        }

        [Fact]
        public void CardOrderController_ShouldHaveApiControllerAndRouteAttributes()
        {
            var type = typeof(CardOrderController);
            
            var apiController = type.GetCustomAttributes(typeof(ApiControllerAttribute), true);
            Assert.NotEmpty(apiController);

            var routeAttributes = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().ToList();
            Assert.NotEmpty(routeAttributes);
            Assert.Equal("api/card-orders", routeAttributes.First().Template);
        }

        [Fact]
        public async Task GetPricing_CallsService_AndReturnsOk()
        {
            var pricing = new OrderPricingResponseDto();
            _cardPricingService.CalculateOrderPricingAsync(CardType.Plastic, 5).Returns(ServiceResult<OrderPricingResponseDto>.Success(pricing));

            var result = await _sut.GetPricing(CardType.Plastic, 5) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardPricingService.Received(1).CalculateOrderPricingAsync(CardType.Plastic, 5);
        }

        [Fact]
        public async Task GetActivePricingCatalog_CallsService_AndReturnsOk()
        {
            var catalog = new List<CardPricingDto>();
            _cardPricingService.GetActiveCatalogAsync().Returns(ServiceResult<IReadOnlyList<CardPricingDto>>.Success(catalog));

            var result = await _sut.GetActivePricingCatalog() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardPricingService.Received(1).GetActiveCatalogAsync();
        }

        [Fact]
        public async Task GetPaged_CallsService_AndReturnsOk()
        {
            var request = new PaginationRequest();
            var paged = PagedResult<CardOrderDto>.Create(new List<CardOrderDto>(), 0, 1, 10);
            _cardOrderService.GetPagedOrdersAsync(request, "PendingReview").Returns(ServiceResult<PagedResult<CardOrderDto>>.Success(paged));

            var result = await _sut.GetPaged(request, "PendingReview") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetPagedOrdersAsync(request, "PendingReview");
        }

        [Fact]
        public async Task GetById_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var order = new CardOrderDto { Id = id };
            _cardOrderService.GetOrderByIdAsync(id).Returns(ServiceResult<CardOrderDto>.Success(order));

            var result = await _sut.GetById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetOrderByIdAsync(id);
        }

        [Fact]
        public async Task GetById_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardOrderService.GetOrderByIdAsync(id).Returns(ServiceResult<CardOrderDto>.Fail("Not found", 404));

            var result = await _sut.GetById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Create_CallsService_AndReturnsStatusCode()
        {
            var request = new CreateCardOrderRequest();
            var dto = new CardOrderDto { Id = Guid.NewGuid() };
            _cardOrderService.CreateOrderAsync(request).Returns(new TestServiceResult<CardOrderDto> { IsSuccess = true, Data = dto, StatusCode = 201 });

            var result = await _sut.Create(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            await _cardOrderService.Received(1).CreateOrderAsync(request);
        }

        [Fact]
        public async Task Create_ReturnsErrorStatusCode_OnFailure()
        {
            var request = new CreateCardOrderRequest();
            _cardOrderService.CreateOrderAsync(request).Returns(ServiceResult<CardOrderDto>.Fail("Error", 400));

            var result = await _sut.Create(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Reorder_CallsService_AndReturnsStatusCode()
        {
            var id = Guid.NewGuid();
            var request = new ReorderRequest();
            var dto = new CardOrderDto { Id = Guid.NewGuid() };
            _cardOrderService.CreateReorderAsync(id, request).Returns(new TestServiceResult<CardOrderDto> { IsSuccess = true, Data = dto, StatusCode = 201 });

            var result = await _sut.Reorder(id, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            await _cardOrderService.Received(1).CreateReorderAsync(id, request);
        }

        [Fact]
        public async Task Reorder_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            var request = new ReorderRequest();
            _cardOrderService.CreateReorderAsync(id, request).Returns(ServiceResult<CardOrderDto>.Fail("Error", 400));

            var result = await _sut.Reorder(id, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Delete_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            _cardOrderService.DeleteOrderAsync(id).Returns(ServiceResult.Success());

            var result = await _sut.Delete(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).DeleteOrderAsync(id);
        }

        [Fact]
        public async Task Delete_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardOrderService.DeleteOrderAsync(id).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.Delete(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetEmployeesImportStatus_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var status = new EmployeesImportStatusDto();
            _employeeImportService.GetImportStatusAsync(id).Returns(ServiceResult<EmployeesImportStatusDto>.Success(status));

            var result = await _sut.GetEmployeesImportStatus(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _employeeImportService.Received(1).GetImportStatusAsync(id);
        }

        [Fact]
        public async Task GetEmployeesImportStatus_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _employeeImportService.GetImportStatusAsync(id).Returns(ServiceResult<EmployeesImportStatusDto>.Fail("Error", 404));

            var result = await _sut.GetEmployeesImportStatus(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }
        [Fact]
        public async Task ResendDeliveryOtp_ReturnsOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            _cardOrderService.ResendOrderOtpAsync(id).Returns(ServiceResult<bool>.Success(true));

            var result = await _sut.ResendDeliveryOtp(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task ResendDeliveryOtp_ReturnsBadRequest_WhenFail()
        {
            var id = Guid.NewGuid();
            _cardOrderService.ResendOrderOtpAsync(id).Returns(ServiceResult<bool>.Fail("Error"));

            var result = await _sut.ResendDeliveryOtp(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }
    }
}


