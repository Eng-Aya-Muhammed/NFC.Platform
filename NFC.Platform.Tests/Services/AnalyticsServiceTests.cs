using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

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
        private readonly IGenericRepository<UserSubscription> _subRepo;

        private readonly AnalyticsService _sut;

        public AnalyticsServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _metricRepo = Substitute.For<IGenericRepository<ProfileMetric>>();
            _employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            _subRepo = Substitute.For<IGenericRepository<UserSubscription>>();

            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);
            _unitOfWork.Repository<ProfileMetric>().Returns(_metricRepo);
            _unitOfWork.Repository<Employee>().Returns(_employeeRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subRepo);

            _messageService.Get(Arg.Any<string>()).Returns(x => (string)x[0]);

            _sut = new AnalyticsService(_unitOfWork, _messageService, _currentTenant);
        }

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
        }

        [Fact]
        public async Task GetUserAnalyticsSummaryAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _profileRepo.GetQueryable().Returns(new List<UserProfile>().BuildMock());

            // Act
            var result = await _sut.GetUserAnalyticsSummaryAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetUserAnalyticsSummaryAsync_ReturnsSummaryData_WhenProfileExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var profileId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var profile = new UserProfile { Id = profileId, UserId = userId };
            _profileRepo.GetQueryable().Returns(new List<UserProfile> { profile }.BuildMock());
            _subRepo.GetQueryable().Returns(new List<UserSubscription>().BuildMock());

            var metrics = new List<ProfileMetric>();
            for (int i = 0; i < 10; i++)
                metrics.Add(new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow });
            for (int i = 0; i < 5; i++)
                metrics.Add(new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ContactSaved, CreatedAt = DateTime.UtcNow });
            for (int i = 0; i < 3; i++)
                metrics.Add(new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.LinkClick, CreatedAt = DateTime.UtcNow });

            _metricRepo.GetQueryable().Returns(metrics.BuildMock());

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
            _profileRepo.GetQueryable().Returns(new List<UserProfile>().BuildMock());

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
            _profileRepo.GetQueryable().Returns(new List<UserProfile> { profile }.BuildMock());

            var metrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ContactSaved, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };
            _metricRepo.GetQueryable().Returns(metrics.BuildMock());

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
            _profileRepo.GetQueryable().Returns(new List<UserProfile> { profile }.BuildMock());

            var metrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profileId, InteractionType = InteractionType.ContactSaved, CreatedAt = DateTime.UtcNow.AddMonths(-1) }
            };
            _metricRepo.GetQueryable().Returns(metrics.BuildMock());

            // Act
            var result = await _sut.GetUserAnalyticsTimeSeriesAsync("monthly");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("monthly", result.Data!.Granularity);
            Assert.Equal(6, result.Data.DataPoints.Count);
        }

        [Fact]
        public async Task GetCompanyDashboardAnalyticsAsync_ReturnsUnauthorized_WhenTenantIdIsNull()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            // Act
            var result = await _sut.GetCompanyDashboardAnalyticsAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task GetCompanyDashboardAnalyticsAsync_ReturnsCorrectData_WhenDataExists()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var emp1 = new Employee { Id = Guid.NewGuid(), TenantId = tenantId, FullName = "Ahmed", JobTitle = "Dev", Department = "IT" };
            var emp2 = new Employee { Id = Guid.NewGuid(), TenantId = tenantId, FullName = "Ali", JobTitle = "HR", Department = "HR" };

            var employees = new List<Employee> { emp1, emp2 };
            _employeeRepo.GetQueryable().Returns(employees.BuildMock());

            var profile1 = new UserProfile { Id = Guid.NewGuid(), Employee = emp1, FullName = "Ahmed" };
            var profile2 = new UserProfile { Id = Guid.NewGuid(), Employee = emp2, FullName = "Ali" };
            var profiles = new List<UserProfile> { profile1, profile2 };
            _profileRepo.GetQueryable().Returns(profiles.BuildMock());

            var metrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfileId = profile1.Id, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profile1.Id, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profile2.Id, InteractionType = InteractionType.ProfileView, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profile1.Id, InteractionType = InteractionType.ContactSaved, CreatedAt = DateTime.UtcNow }
            };
            _metricRepo.GetQueryable().Returns(metrics.BuildMock());

            // Act
            var result = await _sut.GetCompanyDashboardAnalyticsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.TotalEmployees);
            Assert.Equal(1, result.Data.TotalContactSaves);
            
            // emp1 has 2 views, emp2 has 1 view -> emp1 is MostVisited
            Assert.NotNull(result.Data.MostVisitedEmployee);
            Assert.Equal("Ahmed", result.Data.MostVisitedEmployee!.FullName);
            Assert.Equal(2, result.Data.MostVisitedEmployee.TotalViews);

            // Time series should have 12 months
            Assert.Equal(12, result.Data.TimeSeriesData.Count);
            // The current month should have 3 total views (2 for emp1 + 1 for emp2)
            Assert.Equal(3, result.Data.TimeSeriesData.Last().ViewsCount);
        }
    }
}
