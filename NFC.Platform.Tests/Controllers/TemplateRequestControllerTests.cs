namespace NFC.Platform.Tests.Controllers
{
    public class TemplateRequestControllerTests
    {
        private readonly ITemplateRequestService _templateRequestService;
        private readonly ICurrentTenant _currentTenant;
        private readonly TemplateRequestController _sut;

        public TemplateRequestControllerTests()
        {
            _templateRequestService = Substitute.For<ITemplateRequestService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _sut = new TemplateRequestController(_templateRequestService, _currentTenant);
        }

        [Fact]
        public void TemplateRequestController_ShouldHaveAuthorizeAttribute()
        {
            var type = typeof(TemplateRequestController);
            var authorizeAttributes = type.GetCustomAttributes(typeof(AuthorizeAttribute), true);
            Assert.NotEmpty(authorizeAttributes);

            var apiControllerAttributes = type.GetCustomAttributes(typeof(ApiControllerAttribute), true);
            Assert.NotEmpty(apiControllerAttributes);
        }

        [Fact]
        public async Task CreateRequest_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            _currentTenant.UserId.Returns((Guid?)null);

            var result = await _sut.CreateRequest(new CreateTemplateRequest()) as UnauthorizedResult;

            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateRequest_CallsService_AndReturnsStatusCode_OnSuccess()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var request = new CreateTemplateRequest { TemplateName = "Premium" };
            var dto = new TemplateRequestDto { TemplateName = "Premium" };

            _templateRequestService.CreateRequestAsync(userId, request).Returns(new TestServiceResult<TemplateRequestDto> { IsSuccess = true, Data = dto, StatusCode = 201 });

            var result = await _sut.CreateRequest(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            await _templateRequestService.Received(1).CreateRequestAsync(userId, request);
        }

        [Fact]
        public async Task CreateRequest_ReturnsErrorStatusCode_OnFailure()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var request = new CreateTemplateRequest();

            _templateRequestService.CreateRequestAsync(userId, request).Returns(ServiceResult<TemplateRequestDto>.Fail("Error", 400));

            var result = await _sut.CreateRequest(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetTenantRequests_CallsService_AndReturnsOk()
        {
            var list = new List<TemplateRequestDto>();
            _templateRequestService.GetTenantRequestsAsync().Returns(ServiceResult<IReadOnlyList<TemplateRequestDto>>.Success(list));

            var result = await _sut.GetTenantRequests() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _templateRequestService.Received(1).GetTenantRequestsAsync();
        }

        [Fact]
        public async Task GetRequestById_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new TemplateRequestDto { Id = id };
            _templateRequestService.GetRequestByIdAsync(id).Returns(ServiceResult<TemplateRequestDto>.Success(dto));

            var result = await _sut.GetRequestById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _templateRequestService.Received(1).GetRequestByIdAsync(id);
        }

        [Fact]
        public async Task GetRequestById_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _templateRequestService.GetRequestByIdAsync(id).Returns(ServiceResult<TemplateRequestDto>.Fail("Not found", 404));

            var result = await _sut.GetRequestById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }
    }
}
