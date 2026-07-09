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
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class EmployeeServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;
        private readonly IGenericRepository<UserRole> _userRoleRepo;
        private readonly IGenericRepository<Role> _roleRepo;
        private readonly IGenericRepository<Company> _companyRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;

        private readonly EmployeeService _sut;

        public EmployeeServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _userRepo = Substitute.For<IGenericRepository<User>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _userRoleRepo = Substitute.For<IGenericRepository<UserRole>>();
            _roleRepo = Substitute.For<IGenericRepository<Role>>();
            _companyRepo = Substitute.For<IGenericRepository<Company>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();

            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);
            _unitOfWork.Repository<UserRole>().Returns(_userRoleRepo);
            _unitOfWork.Repository<Role>().Returns(_roleRepo);
            _unitOfWork.Repository<Company>().Returns(_companyRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);

            _sut = new EmployeeService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            var request = new CreateEmployeeRequest { Username = "test" };

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsFail_WhenCompanyNotFound()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var queryableCompany = new List<Company>().BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var request = new CreateEmployeeRequest { Username = "test" };

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Company not found for this tenant.", result.Message);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsFail_WhenSubscriptionExpiredOrMissing()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid() };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var queryableSub = new List<UserSubscription>().BuildMock(); // No active sub
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            var request = new CreateEmployeeRequest { Username = "test" };

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("SubscriptionExpiredOrMissing", result.Message);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsFail_WhenMaxEmployeesLimitReached()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid() };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var plan = new SubscriptionPlan { MaxEmployees = 2 };
            var subscription = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10), SubscriptionPlan = plan };
            var queryableSub = new List<UserSubscription> { subscription }.BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            // Count is 2 (which is equal to MaxEmployees = 2)
            _userRepo.CountAsync(Arg.Any<Expression<Func<User, bool>>>()).Returns(2);

            var request = new CreateEmployeeRequest { Username = "test" };

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("MaxEmployeesLimitReached", result.Message);
        }

        [Fact]
        public async Task CreateEmployeeAsync_Success_CreatesUserAndAssignsRole()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid(), Name = "OnPoint" };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var plan = new SubscriptionPlan { MaxEmployees = 100 };
            var subscription = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10), SubscriptionPlan = plan };
            var queryableSub = new List<UserSubscription> { subscription }.BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            _userRepo.CountAsync(Arg.Any<Expression<Func<User, bool>>>()).Returns(5);
            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>()).Returns(new List<User>());

            var request = new CreateEmployeeRequest
            {
                Username = "new.employee",
                Email = "new@onpoint.com",
                FullName = "New Employee",
                JobTitle = "Engineer",
                Department = "IT"
            };

            var role = new Role { Id = Guid.NewGuid(), Name = AppRole.Employee.ToString() };
            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role> { role });

            var mappedDto = new EmployeeDetailsDto { Username = "new.employee" };
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<User>()).Returns(mappedDto);
            _messageService.Get("RecordCreated").Returns("Employee created.");

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Data.TemporaryPassword);
            Assert.Equal(8, result.Data.TemporaryPassword.Length);

            await _unitOfWork.Received(1).BeginTransactionAsync();
            await _userRepo.Received(1).AddAsync(Arg.Any<User>());
            await _userProfileRepo.Received(1).AddAsync(Arg.Any<UserProfile>());
            await _userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>());
            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        [Fact]
        public async Task UpdateEmployeeJobDetailsAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var queryable = new List<User>().BuildMock();
            _userRepo.GetQueryable().Returns(queryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var request = new UpdateEmployeeRequest { Status = UserStatus.Suspended };

            // Act
            var result = await _sut.UpdateEmployeeJobDetailsAsync(Guid.NewGuid(), request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task SoftDeleteEmployeeAsync_RemovesUserRecord_WhenValid()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = new User { Id = id, AccountType = AccountType.Employee };
            _userRepo.GetByIdAsync(id).Returns(user);
            _messageService.Get("RecordDeleted").Returns("Record deleted.");

            // Act
            var result = await _sut.SoftDeleteEmployeeAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            _userRepo.Received(1).Remove(user);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
