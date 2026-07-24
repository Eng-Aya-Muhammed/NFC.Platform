namespace NFC.Platform.Tests.Controllers
{
    public class CompanyControllerTests
    {
        private readonly ICompanyService _companyService;
        private readonly CompanyController _sut;

        public CompanyControllerTests()
        {
            _companyService = Substitute.For<ICompanyService>();
            _sut = new CompanyController(_companyService);
        }

        [Fact]
        public void CompanyController_ShouldHaveHasPermissionAttributeWithCompanyView()
        {
            var type = typeof(CompanyController);
            var auth = type.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
            Assert.NotNull(auth);
            Assert.Equal($"Permission:{AppPermissions.Company.View}", auth.Policy);
        }

        [Fact]
        public async Task GetDashboard_CallsService_AndReturnsOk_OnSuccess()
        {
            var dto = new CompanyDashboardDto();
            _companyService.GetCompanyDashboardAsync().Returns(ServiceResult<CompanyDashboardDto>.Success(dto));

            var result = await _sut.GetDashboard() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _companyService.Received(1).GetCompanyDashboardAsync();
        }

        [Fact]
        public async Task GetDashboard_ReturnsError_OnFailure()
        {
            _companyService.GetCompanyDashboardAsync().Returns(ServiceResult<CompanyDashboardDto>.Fail("Error", 400));

            var result = await _sut.GetDashboard() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetProfile_CallsService_AndReturnsOk_OnSuccess()
        {
            var dto = new CompanyProfileDto();
            _companyService.GetMyCompanyProfileAsync().Returns(ServiceResult<CompanyProfileDto>.Success(dto));

            var result = await _sut.GetProfile() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _companyService.Received(1).GetMyCompanyProfileAsync();
        }

        [Fact]
        public async Task GetProfile_ReturnsError_OnFailure()
        {
            _companyService.GetMyCompanyProfileAsync().Returns(ServiceResult<CompanyProfileDto>.Fail("Error", 404));

            var result = await _sut.GetProfile() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new UpdateCompanyProfileRequest();
            var dto = new CompanyProfileDto();
            _companyService.UpdateCompanyProfileAsync(request).Returns(ServiceResult<CompanyProfileDto>.Success(dto));

            var result = await _sut.UpdateProfile(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _companyService.Received(1).UpdateCompanyProfileAsync(request);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsError_OnFailure()
        {
            var request = new UpdateCompanyProfileRequest();
            _companyService.UpdateCompanyProfileAsync(request).Returns(ServiceResult<CompanyProfileDto>.Fail("Error", 400));

            var result = await _sut.UpdateProfile(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new CompanyChangePasswordRequest();
            _companyService.ChangeCompanyAdminPasswordAsync(request).Returns(ServiceResult.Success());

            var result = await _sut.ChangePassword(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _companyService.Received(1).ChangeCompanyAdminPasswordAsync(request);
        }

        [Fact]
        public async Task ChangePassword_ReturnsError_OnFailure()
        {
            var request = new CompanyChangePasswordRequest();
            _companyService.ChangeCompanyAdminPasswordAsync(request).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.ChangePassword(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void ChangePassword_ShouldHaveRateLimitingPolicy()
        {
            var method = typeof(CompanyController).GetMethod(nameof(CompanyController.ChangePassword));
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
                .Cast<EnableRateLimitingAttribute>()
                .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal("ChangePasswordPolicy", attr.PolicyName);
        }

        [Theory]
        [InlineData(nameof(CompanyController.GetDashboard))]
        [InlineData(nameof(CompanyController.GetProfile))]
        [InlineData(nameof(CompanyController.UpdateProfile))]
        public void NonSensitiveCompanyEndpoints_ShouldNotHaveRateLimiting(string methodName)
        {
            var method = typeof(CompanyController).GetMethod(methodName);
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true);
            Assert.Empty(attr);
        }
        [Fact]
        public async Task ApplyCompanyPublicProfileTemplate_ReturnsOk_OnSuccess()
        {
            var templateId = Guid.NewGuid();
            _companyService.UpdateCompanyTemplateAsync(templateId).Returns(ServiceResult<CompanyProfileDto>.Success(new CompanyProfileDto()));

            var result = await _sut.ApplyCompanyPublicProfileTemplate(templateId) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _companyService.Received(1).UpdateCompanyTemplateAsync(templateId);
        }

        [Fact]
        public async Task RemoveCompanyPublicProfileTemplate_ReturnsOk_OnSuccess()
        {
            _companyService.UpdateCompanyTemplateAsync(null).Returns(ServiceResult<CompanyProfileDto>.Success(new CompanyProfileDto()));

            var result = await _sut.RemoveCompanyPublicProfileTemplate() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _companyService.Received(1).UpdateCompanyTemplateAsync(null);
        }
    }
}
