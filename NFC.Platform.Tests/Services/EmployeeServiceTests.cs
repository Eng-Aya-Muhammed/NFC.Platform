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
using NFC.Platform.BuildingBlocks.Results;
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

        private readonly IGenericRepository<Employee> _employeeRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;
        private readonly IGenericRepository<Company> _companyRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;

        private readonly EmployeeService _sut;

        public EmployeeServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            _userRepo = Substitute.For<IGenericRepository<User>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _companyRepo = Substitute.For<IGenericRepository<Company>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();

            _unitOfWork.Repository<Employee>().Returns(_employeeRepo);
            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);
            _unitOfWork.Repository<Company>().Returns(_companyRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);

            _sut = new EmployeeService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            var request = new CreateEmployeeRequest { Email = "test@test.com" };

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

            var request = new CreateEmployeeRequest { Email = "test@test.com" };

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

            var request = new CreateEmployeeRequest { Email = "test@test.com" };

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
            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(2);

            var request = new CreateEmployeeRequest { Email = "test@test.com" };

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("MaxEmployeesLimitReached", result.Message);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsFail_WhenEmployeeAlreadyExistsWithEmail()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid() };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var plan = new SubscriptionPlan { MaxEmployees = 10 };
            var subscription = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10), SubscriptionPlan = plan };
            var queryableSub = new List<UserSubscription> { subscription }.BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(1);

            // Mock that an employee with same email already exists
            var existingEmployee = new Employee { Email = "duplicate@onpoint.com" };
            _employeeRepo.FindAsync(Arg.Any<Expression<Func<Employee, bool>>>())
                .Returns(new List<Employee> { existingEmployee });

            _messageService.Get("UserAlreadyExists").Returns("User already exists.");

            var request = new CreateEmployeeRequest { Email = "duplicate@onpoint.com" };

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User already exists.", result.Message);
        }

        [Fact]
        public async Task CreateEmployeeAsync_Success_CreatesEmployeeAndProfile()
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

            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(5);
            _employeeRepo.FindAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(new List<Employee>());

            var request = new CreateEmployeeRequest
            {
                Email = "new@onpoint.com",
                FullName = "New Employee",
                JobTitle = "Engineer",
                Department = "IT",
                ProfilePictureUrl = "http://test.com/pic.jpg",
                Phone = "+965 1234 5678",
                WhatsApp = "+965 8765 4321",
                InstagramUrl = "https://instagram.com/new",
                FacebookUrl = "https://facebook.com/new",
                LinkedInUrl = "https://linkedin.com/new",
                WebsiteUrl = "https://new.com",
                CustomLinks = "https://github.com/new\r\nhttps://twitter.com/new"
            };

            var mappedDto = new EmployeeDetailsDto { FullName = "New Employee" };
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<Employee>()).Returns(mappedDto);
            _messageService.Get("RecordCreated").Returns("Employee created.");

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);

            await _unitOfWork.Received(1).BeginTransactionAsync();
            await _employeeRepo.Received(1).AddAsync(Arg.Is<Employee>(e =>
                e.FullName == request.FullName &&
                e.Email == request.Email &&
                e.JobTitle == request.JobTitle &&
                e.Department == request.Department));

            await _userProfileRepo.Received(1).AddAsync(Arg.Is<UserProfile>(p =>
                p.FullName == request.FullName &&
                p.JobTitle == request.JobTitle &&
                p.Department == request.Department &&
                p.ProfilePictureUrl == request.ProfilePictureUrl &&
                p.Phone == request.Phone &&
                p.WhatsApp == request.WhatsApp &&
                p.InstagramUrl == request.InstagramUrl &&
                p.FacebookUrl == request.FacebookUrl &&
                p.LinkedInUrl == request.LinkedInUrl &&
                p.WebsiteUrl == request.WebsiteUrl &&
                p.CustomLinks.Count == 2 &&
                p.CustomLinks.First().Url == "https://github.com/new" &&
                p.CustomLinks.First().Title == "https://github.com/new" &&
                p.CustomLinks.Last().Url == "https://twitter.com/new" &&
                p.CustomLinks.Last().Title == "https://twitter.com/new"));

            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithCloudinaryProfilePicture_MapsUrlCorrectlyToUserProfile()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid(), Name = "CloudCompany" };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var plan = new SubscriptionPlan { MaxEmployees = 50 };
            var subscription = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(30), SubscriptionPlan = plan };
            var queryableSub = new List<UserSubscription> { subscription }.BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(0);
            _employeeRepo.FindAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(new List<Employee>());

            var cloudinaryUrl = "https://res.cloudinary.com/demo/image/upload/v1571218039/nfc-platform/no-tenant/no-user/profile-pics/employee-avatar.png";
            var request = new CreateEmployeeRequest
            {
                Email = "cloudinary.emp@test.com",
                FullName = "Cloudinary Employee",
                JobTitle = "Staff",
                Department = "Operations",
                ProfilePictureUrl = cloudinaryUrl
            };

            var mappedDto = new EmployeeDetailsDto { FullName = "Cloudinary Employee" };
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<Employee>()).Returns(mappedDto);
            _messageService.Get("RecordCreated").Returns("Employee created.");

            // Act
            var result = await _sut.CreateEmployeeAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);

            await _userProfileRepo.Received(1).AddAsync(Arg.Is<UserProfile>(p =>
                p.FullName == request.FullName &&
                p.ProfilePictureUrl == cloudinaryUrl));

            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        [Fact]
        public async Task GetPagedEmployeesAsync_ReturnsSuccess_WithPagedEmployees()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var employeeList = new List<Employee>
            {
                new() { Id = Guid.NewGuid(), FullName = "Emp 1", Email = "emp1@test.com", CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), FullName = "Emp 2", Email = "emp2@test.com", CreatedAt = DateTime.UtcNow }
            };

            var queryable = employeeList.AsQueryable().BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };
            _mapper.Map<EmployeeDto>(Arg.Any<Employee>()).Returns(new EmployeeDto());

            // Act
            var result = await _sut.GetPagedEmployeesAsync(request, "Emp");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalCount);
        }

        [Fact]
        public async Task GetEmployeeDetailsAsync_ReturnsNotFound_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var queryable = new List<Employee>().BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.GetEmployeeDetailsAsync(Guid.NewGuid());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetEmployeeDetailsAsync_ReturnsSuccess_WithEmployeeDetails()
        {
            // Arrange
            var id = Guid.NewGuid();
            var employee = new Employee { Id = id, FullName = "John Doe" };
            var queryable = new List<Employee> { employee }.BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);

            var expectedDto = new EmployeeDetailsDto { Id = id, FullName = "John Doe" };
            _mapper.Map<EmployeeDetailsDto>(employee).Returns(expectedDto);

            // Act
            var result = await _sut.GetEmployeeDetailsAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Data!.Id);
        }

        [Fact]
        public async Task UpdateEmployeeJobDetailsAsync_ReturnsNotFound_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var queryable = new List<Employee>().BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var request = new UpdateEmployeeRequest { Status = UserStatus.Suspended };

            // Act
            var result = await _sut.UpdateEmployeeJobDetailsAsync(Guid.NewGuid(), request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateEmployeeJobDetailsAsync_Success_UpdatesEmployeeAndUserProfile()
        {
            // Arrange
            var id = Guid.NewGuid();
            var employee = new Employee
            {
                Id = id,
                Status = UserStatus.Active,
                JobTitle = "Old Title",
                UserProfile = new UserProfile { JobTitle = "Old Title" }
            };

            var queryable = new List<Employee> { employee }.BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);

            var request = new UpdateEmployeeRequest
            {
                JobTitle = "New Title",
                Department = "New Dept",
                Status = UserStatus.Active
            };

            _mapper.Map<EmployeeDetailsDto>(employee).Returns(new EmployeeDetailsDto { JobTitle = "New Title" });
            _messageService.Get("RecordUpdated").Returns("Updated successfully.");

            // Act
            var result = await _sut.UpdateEmployeeJobDetailsAsync(id, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("New Title", employee.JobTitle);
            Assert.Equal("New Title", employee.UserProfile!.JobTitle);
            _employeeRepo.Received(1).Update(employee);
            _userProfileRepo.Received(1).Update(employee.UserProfile!);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task SoftDeleteEmployeeAsync_ReturnsNotFound_WhenEmployeeDoesNotExist()
        {
            // Arrange
            _employeeRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Employee)null!);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.SoftDeleteEmployeeAsync(Guid.NewGuid());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task SoftDeleteEmployeeAsync_RemovesEmployeeRecord_WhenValid()
        {
            // Arrange
            var id = Guid.NewGuid();
            var employee = new Employee { Id = id };
            _employeeRepo.GetByIdAsync(id).Returns(employee);
            _messageService.Get("RecordDeleted").Returns("Record deleted.");

            // Act
            var result = await _sut.SoftDeleteEmployeeAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            _employeeRepo.Received(1).Remove(employee);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
