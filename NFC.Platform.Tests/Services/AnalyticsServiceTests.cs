namespace NFC.Platform.Tests.Services
{
    public class AnalyticsServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<ProfileMetric> _metricRepo;

        private readonly IGenericRepository<Employee> _employeeRepo;

        private readonly AnalyticsService _sut;

        public AnalyticsServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _metricRepo = Substitute.For<IGenericRepository<ProfileMetric>>();

            _employeeRepo = Substitute.For<IGenericRepository<Employee>>();

            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);
            _unitOfWork.Repository<ProfileMetric>().Returns(_metricRepo);

            _unitOfWork.Repository<Employee>().Returns(_employeeRepo);

            _messageService.Get(Arg.Any<string>()).Returns(x => (string)x[0]);

            _sut = new AnalyticsService(_unitOfWork, _messageService, _currentTenant);
        }

        // ── GetUserAnalyticsSummaryAsync ──────────────────────────────────────────

        [Fact]
        public async Task GetUserAnalyticsSummaryAsync_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);

            // Act
            var result = await _sut.GetUserAnalyticsSummaryAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            Assert.Equal("UserNotAuthenticated", result.Message);
        }

        [Fact]
        public async Task GetUserAnalyticsSummaryAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _profileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetUserAnalyticsSummaryAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("ProfileNotFound", result.Message);
        }

        [Fact]
        public async Task GetUserAnalyticsSummaryAsync_ReturnsSummaryData_WhenProfileExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var profileId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var profile = new UserProfile { Id = profileId, UserId = userId };
            _profileRepo.GetQueryable().Returns(new List<UserProfile> { profile }.AsQueryable().BuildMock());

            _metricRepo.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ProfileMetric, bool>>>())
                .Returns(x =>
                {
                    var expr = x.ArgAt<System.Linq.Expressions.Expression<Func<ProfileMetric, bool>>>(0).ToString();
                    if (expr.Contains("ProfileView") || expr.Contains("== 1")) return Task.FromResult(10);
                    if (expr.Contains("ContactSaved") || expr.Contains("== 2")) return Task.FromResult(5);
                    if (expr.Contains("LinkClick") || expr.Contains("== 3")) return Task.FromResult(3);
                    return Task.FromResult(0);
                });



            // Set up monthly views (last 6 months)
            var metrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow.AddMonths(-1) }
            };
            _metricRepo.GetQueryable().Returns(metrics.AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetUserAnalyticsSummaryAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(10, result.Data.TotalProfileViews);
            Assert.Equal(5, result.Data.TotalContactSaves);
            Assert.Equal(3, result.Data.TotalLinkClicks);

            Assert.Equal(6, result.Data.MonthlyViews.Count);
        }

        // ── GetUserAnalyticsTimeSeriesAsync ───────────────────────────────────────

        [Fact]
        public async Task GetUserAnalyticsTimeSeriesAsync_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);

            // Act
            var result = await _sut.GetUserAnalyticsTimeSeriesAsync("daily");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task GetUserAnalyticsTimeSeriesAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _profileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetUserAnalyticsTimeSeriesAsync("daily");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetUserAnalyticsTimeSeriesAsync_ReturnsDailyMetrics_WhenGranularityIsDaily()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var profileId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var profile = new UserProfile { Id = profileId, UserId = userId };
            _profileRepo.GetQueryable().Returns(new List<UserProfile> { profile }.AsQueryable().BuildMock());

            var metrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ContactSaved, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };
            _metricRepo.GetQueryable().Returns(metrics.AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetUserAnalyticsTimeSeriesAsync("daily");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("daily", result.Data!.Granularity);
            Assert.Equal(30, result.Data.DataPoints.Count);
        }

        [Fact]
        public async Task GetUserAnalyticsTimeSeriesAsync_ReturnsMonthlyMetrics_WhenGranularityIsMonthly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var profileId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var profile = new UserProfile { Id = profileId, UserId = userId };
            _profileRepo.GetQueryable().Returns(new List<UserProfile> { profile }.AsQueryable().BuildMock());

            var metrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ContactSaved, CreatedAt = DateTime.UtcNow.AddMonths(-1) }
            };
            _metricRepo.GetQueryable().Returns(metrics.AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetUserAnalyticsTimeSeriesAsync("monthly");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("monthly", result.Data!.Granularity);
            Assert.Equal(6, result.Data.DataPoints.Count);
        }

        // ── GetCompanyLeaderboardAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetCompanyLeaderboardAsync_ReturnsUnauthorized_WhenTenantIdIsNull()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            // Act
            var result = await _sut.GetCompanyLeaderboardAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task GetCompanyLeaderboardAsync_ReturnsEmptyList_WhenNoEmployeesExist()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            _employeeRepo.GetQueryable().Returns(new List<Employee>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetCompanyLeaderboardAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetCompanyLeaderboardAsync_ReturnsRankedLeaderboard_WhenEmployeesExist()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var emp1Id = Guid.NewGuid();
            var emp2Id = Guid.NewGuid();
            var employees = new List<Employee>
            {
                new Employee { Id = emp1Id, TenantId = tenantId, FullName = "Emp One", JobTitle = "Dev" },
                new Employee { Id = emp2Id, TenantId = tenantId, FullName = "Emp Two", JobTitle = "QA" }
            };
            _employeeRepo.GetQueryable().Returns(employees.AsQueryable().BuildMock());

            var profile1Id = Guid.NewGuid();
            var profile2Id = Guid.NewGuid();
            var profiles = new List<UserProfile>
            {
                new UserProfile { Id = profile1Id, EmployeeId = emp1Id, FullName = "Emp One" },
                new UserProfile { Id = profile2Id, EmployeeId = emp2Id, FullName = "Emp Two" }
            };
            _profileRepo.GetQueryable().Returns(profiles.AsQueryable().BuildMock());

            var metrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfileId = profile1Id, InteractionType = InteractionType.ProfileView },
                new ProfileMetric { UserProfileId = profile1Id, InteractionType = InteractionType.ContactSaved },
                new ProfileMetric { UserProfileId = profile2Id, InteractionType = InteractionType.ProfileView }
            };
            _metricRepo.GetQueryable().Returns(metrics.AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetCompanyLeaderboardAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);
            
            // Emp One should be ranked 1st because they have 2 interactions (vs 1 for Emp Two)
            Assert.Equal(emp1Id, result.Data[0].EmployeeId);
            Assert.Equal(1, result.Data[0].Rank);
            Assert.Equal(2, result.Data[0].TotalInteractions);

            Assert.Equal(emp2Id, result.Data[1].EmployeeId);
            Assert.Equal(2, result.Data[1].Rank);
            Assert.Equal(1, result.Data[1].TotalInteractions);
        }
    }
}
