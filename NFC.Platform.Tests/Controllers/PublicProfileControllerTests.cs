namespace NFC.Platform.Tests.Controllers
{
    public class PublicProfileControllerTests
    {
        private readonly IProfileMetricService _profileMetricService;
        private readonly PublicProfileController _sut;

        public PublicProfileControllerTests()
        {
            _profileMetricService = Substitute.For<IProfileMetricService>();
            _sut = new PublicProfileController(_profileMetricService);
        }

        [Fact]
        public void PublicProfileController_ShouldHaveAllowAnonymousAndRouteAttributes()
        {
            var type = typeof(PublicProfileController);
            Assert.NotEmpty(type.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
            var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().First();
            Assert.Equal("api/public", route.Template);
        }

        [Fact]
        public async Task ResolvePublicProfile_CallsService_AndReturnsOk_OnSuccess()
        {
            var code = "ABC123XYZ";
            var dto = new EmployeeDetailsDto();
            _profileMetricService.ResolvePublicProfileAsync(code).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));

            var result = await _sut.ResolvePublicProfile(code) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileMetricService.Received(1).ResolvePublicProfileAsync(code);
        }

        [Fact]
        public async Task ResolvePublicProfile_ReturnsError_OnFailure()
        {
            var code = "ABC123XYZ";
            _profileMetricService.ResolvePublicProfileAsync(code).Returns(ServiceResult<EmployeeDetailsDto>.Fail("Error", 404));

            var result = await _sut.ResolvePublicProfile(code) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task RecordMetric_CallsService_AndReturnsOk_OnSuccess()
        {
            var profileId = Guid.NewGuid();
            var request = new RecordMetricRequest();
            _profileMetricService.RecordMetricAsync(profileId, request).Returns(ServiceResult.Success());

            var result = await _sut.RecordMetric(profileId, request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileMetricService.Received(1).RecordMetricAsync(profileId, request);
        }

        [Fact]
        public async Task RecordMetric_ReturnsError_OnFailure()
        {
            var profileId = Guid.NewGuid();
            var request = new RecordMetricRequest();
            _profileMetricService.RecordMetricAsync(profileId, request).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.RecordMetric(profileId, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void ResolvePublicProfile_ShouldHaveRateLimitingPolicy()
        {
            var method = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.ResolvePublicProfile));
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
                .Cast<EnableRateLimitingAttribute>()
                .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal("ResolvePublicProfilePolicy", attr.PolicyName);
        }

        [Fact]
        public void RecordMetric_ShouldNotHaveRateLimiting()
        {
            var method = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.RecordMetric));
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true);
            Assert.Empty(attr);
        }
    }
}
