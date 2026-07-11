using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CompanyServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        private readonly IGenericRepository<Company> _companyRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Employee> _employeeRepo;
        private readonly IGenericRepository<CardOrder> _orderRepo;
        private readonly IGenericRepository<ProfileMetric> _metricRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;

        private readonly CompanyService _sut;

        public CompanyServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _companyRepo = Substitute.For<IGenericRepository<Company>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            _userRepo = Substitute.For<IGenericRepository<User>>();
            _employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            _orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            _metricRepo = Substitute.For<IGenericRepository<ProfileMetric>>();
            _profileRepo = Substitute.For<IGenericRepository<UserProfile>>();

            _unitOfWork.Repository<Company>().Returns(_companyRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);
            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<Employee>().Returns(_employeeRepo);
            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<ProfileMetric>().Returns(_metricRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);

            _sut = new CompanyService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        [Fact]
        public async Task GetMyCompanyProfileAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            // Act
            var result = await _sut.GetMyCompanyProfileAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task GetMyCompanyProfileAsync_ReturnsNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var queryable = new List<Company>().BuildMock();
            _companyRepo.GetQueryable().Returns(queryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.GetMyCompanyProfileAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetMyCompanyProfileAsync_ReturnsSuccess_WithRemainingDaysComputed()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Name = "OnPoint", TenantId = tenantId };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var subscription = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(15).AddHours(1)
            };
            var queryableSub = new List<UserSubscription> { subscription }.BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            var dto = new CompanyProfileDto { Name = "OnPoint" };
            _mapper.Map<CompanyProfileDto>(company).Returns(dto);

            // Act
            var result = await _sut.GetMyCompanyProfileAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(15, result.Data.SubscriptionRemainingDays);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_UpdatesProfileAndReturnsSuccess()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var adminUser = new User { Id = Guid.NewGuid(), PhoneNumber = "123" };
            var company = new Company { Name = "Old OnPoint", TenantId = tenantId, AdminUser = adminUser };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var queryableSub = new List<UserSubscription>().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            var request = new UpdateCompanyProfileRequest { Name = "New OnPoint", Phone = "9999" };
            var dto = new CompanyProfileDto { Name = "New OnPoint", Phone = "9999" };

            _mapper.Map(request, company).Returns(c => {
                company.Name = request.Name;
                return company;
            });
            _mapper.Map<CompanyProfileDto>(company).Returns(dto);
            _messageService.Get("RecordUpdated").Returns("Record updated successfully.");

            // Act
            var result = await _sut.UpdateCompanyProfileAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("New OnPoint", result.Data.Name);
            Assert.Equal("9999", adminUser.PhoneNumber);
            _companyRepo.Received(1).Update(company);
            _userRepo.Received(1).Update(adminUser);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task ChangeCompanyAdminPasswordAsync_ReturnsFail_WhenOldPasswordIncorrect()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var user = new User
            {
                Id = userId,
                PasswordHash = PasswordHasher.HashPassword("CorrectPassword")
            };

            _userRepo.GetByIdAsync(userId).Returns(user);
            _messageService.Get("InvalidCredentials").Returns("Invalid credentials.");

            var request = new CompanyChangePasswordRequest
            {
                OldPassword = "WrongPassword",
                NewPassword = "NewPassword123!"
            };

            // Act
            var result = await _sut.ChangeCompanyAdminPasswordAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetCompanyDashboardAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            // Act
            var result = await _sut.GetCompanyDashboardAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task GetCompanyDashboardAsync_Success_ReturnsAggregatedMetricsAndChart()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            _employeeRepo.CountAsync().Returns(10);
            _orderRepo.CountAsync().Returns(5);
            _metricRepo.CountAsync(Arg.Any<Expression<Func<ProfileMetric, bool>>>()).Returns(100);

            // Mock Top Employee query
            var profileId1 = Guid.NewGuid();
            var profileId2 = Guid.NewGuid();
            var metrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfileId = profileId1, UserProfile = new UserProfile { FullName = "Sara" }, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profileId1, UserProfile = new UserProfile { FullName = "Sara" }, CreatedAt = DateTime.UtcNow },
                new ProfileMetric { UserProfileId = profileId2, UserProfile = new UserProfile { FullName = "John" }, CreatedAt = DateTime.UtcNow }
            };
            _metricRepo.GetQueryable().Returns(metrics.AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetCompanyDashboardAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(10, result.Data.TotalEmployeesCount);
            Assert.Equal(5, result.Data.CardRequestsCount);
            Assert.Equal(100, result.Data.ContactSavesCount);
            Assert.Equal("Sara", result.Data.TopEmployeeName);
            Assert.Equal(6, result.Data.MonthlyMetrics.Count);
        }
    }
}
