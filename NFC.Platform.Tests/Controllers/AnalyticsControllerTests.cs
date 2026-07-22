namespace NFC.Platform.Tests.Controllers
{
    public class AnalyticsControllerTests
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly AnalyticsController _sut;

        public AnalyticsControllerTests()
        {
            _analyticsService = Substitute.For<IAnalyticsService>();
            _sut = new AnalyticsController(_analyticsService);
        }

        [Fact]
        public void AnalyticsController_ShouldHaveAuthorizeAndRouteAttributes()
        {
            var type = typeof(AnalyticsController);
            var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute), true);
            Assert.NotEmpty(auth);

            var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().First();
            Assert.Equal("api/analytics", route.Template);
        }

        [Fact]
        public async Task GetUserAnalyticsSummary_CallsService_AndReturnsOk_OnSuccess()
        {
            var dto = new UserAnalyticsSummaryDto();
            _analyticsService.GetUserAnalyticsSummaryAsync().Returns(ServiceResult<UserAnalyticsSummaryDto>.Success(dto));

            var result = await _sut.GetUserAnalyticsSummary() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _analyticsService.Received(1).GetUserAnalyticsSummaryAsync();
        }

        [Fact]
        public async Task GetUserAnalyticsSummary_ReturnsError_OnFailure()
        {
            _analyticsService.GetUserAnalyticsSummaryAsync().Returns(ServiceResult<UserAnalyticsSummaryDto>.Fail("Error", 400));

            var result = await _sut.GetUserAnalyticsSummary() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetUserAnalyticsTimeSeries_CallsService_AndReturnsOk_OnSuccess()
        {
            var dto = new UserAnalyticsTimeSeriesDto();
            _analyticsService.GetUserAnalyticsTimeSeriesAsync("monthly").Returns(ServiceResult<UserAnalyticsTimeSeriesDto>.Success(dto));

            var result = await _sut.GetUserAnalyticsTimeSeries("monthly") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _analyticsService.Received(1).GetUserAnalyticsTimeSeriesAsync("monthly");
        }

        [Fact]
        public async Task GetUserAnalyticsTimeSeries_ReturnsError_OnFailure()
        {
            _analyticsService.GetUserAnalyticsTimeSeriesAsync("daily").Returns(ServiceResult<UserAnalyticsTimeSeriesDto>.Fail("Error", 400));

            var result = await _sut.GetUserAnalyticsTimeSeries("daily") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetCompanyLeaderboard_CallsService_AndReturnsOk_OnSuccess()
        {
            var list = new List<EmployeeLeaderboardEntryDto>();
            _analyticsService.GetCompanyLeaderboardAsync().Returns(ServiceResult<List<EmployeeLeaderboardEntryDto>>.Success(list));

            var result = await _sut.GetCompanyLeaderboard() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _analyticsService.Received(1).GetCompanyLeaderboardAsync();
        }

        [Fact]
        public async Task GetCompanyLeaderboard_ReturnsError_OnFailure()
        {
            _analyticsService.GetCompanyLeaderboardAsync().Returns(ServiceResult<List<EmployeeLeaderboardEntryDto>>.Fail("Error", 400));

            var result = await _sut.GetCompanyLeaderboard() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void GetCompanyLeaderboard_ShouldHaveAuthorizeAttributeWithCompanyAdminOnlyPolicy()
        {
            var type = typeof(AnalyticsController);
            var method = type.GetMethod(nameof(AnalyticsController.GetCompanyLeaderboard));
            Assert.NotNull(method);

            var auth = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
            Assert.NotNull(auth);
            Assert.Equal(AppPolicies.CompanyAdminOnly, auth.Policy);
        }
    }
}
