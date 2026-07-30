using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable.NSubstitute;
using NSubstitute;
using NFC.Platform.Application.Interfaces;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class EmployeeAnalyticsServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly AnalyticsService _sut;

        public EmployeeAnalyticsServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _sut = new AnalyticsService(_unitOfWork, _messageService, _currentTenant);
        }

        [Fact]
        public async Task GetEmployeeDashboardAnalyticsAsync_ReturnsNotFound_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            _messageService.Get("EmployeeNotFound").Returns("Employee not found.");

            var empRepo = Substitute.For<IGenericRepository<Employee>>();
            empRepo.GetQueryable().Returns(new List<Employee>().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(empRepo);

            // Act
            var result = await _sut.GetEmployeeDashboardAnalyticsAsync(employeeId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetEmployeeDashboardAnalyticsAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            _messageService.Get("ProfileNotFound").Returns("Profile not found.");

            var employee = new Employee { Id = employeeId, TenantId = tenantId };
            var empRepo = Substitute.For<IGenericRepository<Employee>>();
            empRepo.GetQueryable().Returns(new List<Employee> { employee }.BuildMock());
            _unitOfWork.Repository<Employee>().Returns(empRepo);

            var profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            profileRepo.GetQueryable().Returns(new List<UserProfile>().BuildMock());
            _unitOfWork.Repository<UserProfile>().Returns(profileRepo);

            // Act
            var result = await _sut.GetEmployeeDashboardAnalyticsAsync(employeeId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetEmployeeDashboardAnalyticsAsync_Returns_Exact_Figma_Metrics_Calculations()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var profileId = Guid.NewGuid();

            _currentTenant.TenantId.Returns(tenantId);

            var employee = new Employee { Id = employeeId, TenantId = tenantId, FullName = "Ahmed Mohamed", JobTitle = "Engineer" };
            var empRepo = Substitute.For<IGenericRepository<Employee>>();
            empRepo.GetQueryable().Returns(new List<Employee> { employee }.BuildMock());
            _unitOfWork.Repository<Employee>().Returns(empRepo);

            var userProfile = new UserProfile
            {
                Id = profileId,
                EmployeeId = employeeId,
                TenantId = tenantId,
                FullName = "Employee Test"
            };

            var profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            profileRepo.GetQueryable().Returns(new List<UserProfile> { userProfile }.BuildMock());
            _unitOfWork.Repository<UserProfile>().Returns(profileRepo);

            var subscription = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-90),
                EndDate = DateTime.UtcNow.AddDays(20),
                IsDeleted = false
            };

            var subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { subscription }.BuildMock());
            _unitOfWork.Repository<UserSubscription>().Returns(subscriptionRepo);

            var metricsList = new List<ProfileMetric>();

            // Add 500 profile views in the last 30 days
            for (int i = 0; i < 500; i++)
            {
                metricsList.Add(new ProfileMetric
                {
                    UserProfileId = profileId,
                    TenantId = tenantId,
                    InteractionType = InteractionType.ProfileView,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                });
            }

            // Add 352 contact saves in the last 30 days
            for (int i = 0; i < 352; i++)
            {
                metricsList.Add(new ProfileMetric
                {
                    UserProfileId = profileId,
                    TenantId = tenantId,
                    InteractionType = InteractionType.ContactSaved,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                });
            }

            var metricRepo = Substitute.For<IGenericRepository<ProfileMetric>>();
            metricRepo.GetQueryable().Returns(metricsList.BuildMock());
            _unitOfWork.Repository<ProfileMetric>().Returns(metricRepo);

            _messageService.Get("ViewsLabel").Returns("زيارات");

            // Act
            var result = await _sut.GetEmployeeDashboardAnalyticsAsync(employeeId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            Assert.Equal(20, result.Data.SubscriptionRemainingDays);
            Assert.Equal(110, result.Data.TotalSubscriptionDays);
            Assert.Equal(500, result.Data.MonthlyViews);
            Assert.Equal(352, result.Data.ContactSavesCount);
            Assert.Equal(12, result.Data.YearlyViewsTrend.Count);
            Assert.Equal(70.4, result.Data.ContactSaveRate); // (352 / 500) * 100 = 70.4%
            Assert.Equal(500, result.Data.TotalYearlyViews);
            Assert.Equal(1.4, result.Data.AverageDailyViews); // 500 / 365 = 1.369 -> 1.4
            Assert.Equal(500, result.Data.PeakMonth.ViewsCount);
            Assert.Contains("500", result.Data.PeakMonth.FormattedText);
        }
    }
}
