namespace NFC.Platform.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly IAdminService _adminService;
        private readonly AdminController _sut;

        public AdminControllerTests()
        {
            _adminService = Substitute.For<IAdminService>();
            _sut = new AdminController(_adminService);
        }

        [Fact]
        public void AdminController_ShouldHaveAuthorizeAttributeWithAdminOnlyPolicy()
        {
            var type = typeof(AdminController);
            var attributes = type.GetCustomAttributes(typeof(AuthorizeAttribute), true);
            Assert.NotEmpty(attributes);
            var auth = attributes.First() as AuthorizeAttribute;
            Assert.NotNull(auth);
            Assert.Equal(AppPolicies.AdminOnly, auth.Policy);
        }

        [Fact]
        public async Task GetOrdersPaged_CallsAdminService_AndReturnsOk()
        {
            var request = new PaginationRequest();
            var status = OrderStatus.InPrinting;
            var companyId = Guid.NewGuid();
            var expectedResult = ServiceResult<PagedResult<AdminOrderSummaryDto>>.Success(
                PagedResult<AdminOrderSummaryDto>.Create(new List<AdminOrderSummaryDto>(), 0, 1, 10));

            _adminService.GetOrdersPagedAsync(request, status, companyId).Returns(expectedResult);

            var result = await _sut.GetOrdersPaged(request, status, companyId, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _adminService.Received(1).GetOrdersPagedAsync(request, status, companyId);
        }

        [Fact]
        public async Task GetOrdersPaged_WithNullStatusAndCompanyId_CallsAdminService_AndReturnsOk()
        {
            var request = new PaginationRequest();
            var expectedResult = ServiceResult<PagedResult<AdminOrderSummaryDto>>.Success(
                PagedResult<AdminOrderSummaryDto>.Create(new List<AdminOrderSummaryDto>(), 0, 1, 10));

            _adminService.GetOrdersPagedAsync(request, null, null).Returns(expectedResult);

            var result = await _sut.GetOrdersPaged(request, null, null, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _adminService.Received(1).GetOrdersPagedAsync(request, null, null);
        }

        [Fact]
        public async Task GetTemplateRequestsPaged_WithNullStatus_CallsAdminService_AndReturnsOk()
        {
            var request = new PaginationRequest();
            var expectedResult = ServiceResult<PagedResult<TemplateRequestDto>>.Success(
                PagedResult<TemplateRequestDto>.Create(new List<TemplateRequestDto>(), 0, 1, 10));

            _adminService.GetTemplateRequestsPagedAsync(request, null).Returns(expectedResult);

            var result = await _sut.GetTemplateRequestsPaged(request, null, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _adminService.Received(1).GetTemplateRequestsPagedAsync(request, null);
        }

        [Fact]
        public async Task GetOrderById_CallsAdminService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new AdminOrderDetailDto();
            _adminService.GetOrderByIdAsync(id).Returns(ServiceResult<AdminOrderDetailDto>.Success(dto));

            var result = await _sut.GetOrderById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetOrderById_ReturnsError_OnFailure()
        {
            var id = Guid.NewGuid();
            _adminService.GetOrderByIdAsync(id).Returns(ServiceResult<AdminOrderDetailDto>.Fail("Error", 404));

            var result = await _sut.GetOrderById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderStatus_CallsAdminService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateOrderStatusDto();
            _adminService.UpdateOrderStatusAsync(id, dto).Returns(ServiceResult.Success());

            var result = await _sut.UpdateOrderStatus(id, dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderStatus_ReturnsError_OnFailure()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateOrderStatusDto();
            _adminService.UpdateOrderStatusAsync(id, dto).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.UpdateOrderStatus(id, dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetTemplateRequestsPaged_CallsAdminService_AndReturnsOk()
        {
            var request = new PaginationRequest();
            var status = TemplateRequestStatus.Completed;
            var expectedResult = ServiceResult<PagedResult<TemplateRequestDto>>.Success(
                PagedResult<TemplateRequestDto>.Create(new List<TemplateRequestDto>(), 0, 1, 10));

            _adminService.GetTemplateRequestsPagedAsync(request, status).Returns(expectedResult);

            var result = await _sut.GetTemplateRequestsPaged(request, status, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _adminService.Received(1).GetTemplateRequestsPagedAsync(request, status);
        }

        [Fact]
        public async Task ResolveTemplateRequest_CallsAdminService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new ResolveTemplateRequestDto();
            _adminService.ResolveTemplateRequestAsync(id, dto).Returns(ServiceResult.Success());

            var result = await _sut.ResolveTemplateRequest(id, dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task ResolveTemplateRequest_ReturnsError_OnFailure()
        {
            var id = Guid.NewGuid();
            var dto = new ResolveTemplateRequestDto();
            _adminService.ResolveTemplateRequestAsync(id, dto).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.ResolveTemplateRequest(id, dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateTemplate_CallsAdminService_AndReturnsOk()
        {
            var dto = new CreateCardTemplateDto();
            var resultDto = new CardTemplateDto();
            _adminService.CreateTemplateAsync(dto).Returns(ServiceResult<CardTemplateDto>.Success(resultDto));

            var result = await _sut.CreateTemplate(dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _adminService.Received(1).CreateTemplateAsync(dto);
        }

        [Fact]
        public async Task UpdateTemplate_CallsAdminService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateCardTemplateDto();
            var resultDto = new CardTemplateDto();
            _adminService.UpdateTemplateAsync(id, dto).Returns(ServiceResult<CardTemplateDto>.Success(resultDto));

            var result = await _sut.UpdateTemplate(id, dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task UpdateTemplate_ReturnsError_OnFailure()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateCardTemplateDto();
            _adminService.UpdateTemplateAsync(id, dto).Returns(ServiceResult<CardTemplateDto>.Fail("Error", 400));

            var result = await _sut.UpdateTemplate(id, dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task DeleteTemplate_CallsAdminService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            _adminService.DeleteTemplateAsync(id).Returns(ServiceResult.Success());

            var result = await _sut.DeleteTemplate(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task DeleteTemplate_ReturnsError_OnFailure()
        {
            var id = Guid.NewGuid();
            _adminService.DeleteTemplateAsync(id).Returns(ServiceResult.Fail("Error", 404));

            var result = await _sut.DeleteTemplate(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetTenantsPaged_CallsAdminService_AndReturnsOk()
        {
            var request = new PaginationRequest();
            var paged = PagedResult<TenantSummaryDto>.Create(new List<TenantSummaryDto>(), 0, 1, 10);
            _adminService.GetTenantsPagedAsync(request).Returns(ServiceResult<PagedResult<TenantSummaryDto>>.Success(paged));

            var result = await _sut.GetTenantsPaged(request, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _adminService.Received(1).GetTenantsPagedAsync(request);
        }

        [Fact]
        public async Task UpdateTenantStatus_CallsAdminService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateTenantStatusDto();
            _adminService.UpdateTenantStatusAsync(id, dto).Returns(ServiceResult.Success());

            var result = await _sut.UpdateTenantStatus(id, dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task UpdateTenantStatus_ReturnsError_OnFailure()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateTenantStatusDto();
            _adminService.UpdateTenantStatusAsync(id, dto).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.UpdateTenantStatus(id, dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCardPricing_CallsAdminService_AndReturnsOk_OnSuccess()
        {
            var dto = new UpdateCardPricingDto();
            _adminService.UpdateCardPricingAsync(dto).Returns(ServiceResult.Success());

            var result = await _sut.UpdateCardPricing(dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCardPricing_ReturnsError_OnFailure()
        {
            var dto = new UpdateCardPricingDto();
            _adminService.UpdateCardPricingAsync(dto).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.UpdateCardPricing(dto) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }
        [Fact]
        public async Task VerifyDeliveryOtp_ReturnsOk_WhenValid()
        {
            var id = Guid.NewGuid();
            var request = new VerifyDeliveryOtpRequest { Otp = "123456" };
            _adminService.VerifyDeliveryOtpAsync(id, request.Otp).Returns(ServiceResult<bool>.Success(true));

            var result = await _sut.VerifyDeliveryOtp(id, request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task VerifyDeliveryOtp_ReturnsBadRequest_WhenInvalid()
        {
            var id = Guid.NewGuid();
            var request = new VerifyDeliveryOtpRequest { Otp = "123456" };
            _adminService.VerifyDeliveryOtpAsync(id, request.Otp).Returns(ServiceResult<bool>.Fail("Invalid OTP"));

            var result = await _sut.VerifyDeliveryOtp(id, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ResendDeliveryOtp_ReturnsOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            _adminService.ResendDeliveryOtpAsync(id).Returns(ServiceResult<bool>.Success(true));

            var result = await _sut.ResendDeliveryOtp(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetAllSubdomains_ReturnsOk_WithData()
        {
            var request = new PaginationRequest();
            var pagedData = PagedResult<ProfileSubdomainSummaryDto>.Create(new List<ProfileSubdomainSummaryDto>(), 0, 1, 10);
            _adminService.GetAllProfileSubdomainsAsync(request, null, CancellationToken.None).Returns(ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>.Success(pagedData));

            var result = await _sut.GetAllSubdomains(request, null, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task ReassignSubdomain_ReturnsOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new ReassignSubdomainDto { Subdomain = "test" };
            _adminService.ReassignSubdomainAsync(id, dto.Subdomain).Returns(ServiceResult<bool>.Success(true));

            var result = await _sut.ReassignSubdomain(id, dto) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task CreatePlan_ReturnsOk_WhenSuccess()
        {
            var request = new CreateSubscriptionPlanRequest();
            _adminService.CreatePlanAsync(request).Returns(ServiceResult<SubscriptionPlanDto>.Success(new SubscriptionPlanDto()));

            var result = await _sut.CreatePlan(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task UpdatePlan_ReturnsOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            var request = new UpdateSubscriptionPlanRequest();
            _adminService.UpdatePlanAsync(id, request).Returns(ServiceResult<SubscriptionPlanDto>.Success(new SubscriptionPlanDto()));

            var result = await _sut.UpdatePlan(id, request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task DeletePlan_ReturnsOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            _adminService.DeletePlanAsync(id).Returns(ServiceResult<bool>.Success(true));

            var result = await _sut.DeletePlan(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetPlanTemplates_ReturnsOk_WithData()
        {
            var id = Guid.NewGuid();
            _adminService.GetPlanTemplatesAsync(id).Returns(ServiceResult<IReadOnlyList<CardTemplateSummaryDto>>.Success(new List<CardTemplateSummaryDto>()));

            var result = await _sut.GetPlanTemplates(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task AssignTemplate_ReturnsOk_WhenSuccess()
        {
            var planId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            _adminService.AssignTemplateAsync(planId, templateId).Returns(ServiceResult<bool>.Success(true));

            var result = await _sut.AssignTemplate(planId, templateId) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task UnassignTemplate_ReturnsOk_WhenSuccess()
        {
            var planId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            _adminService.UnassignTemplateAsync(planId, templateId).Returns(ServiceResult<bool>.Success(true));

            var result = await _sut.UnassignTemplate(planId, templateId) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }
    }
}



