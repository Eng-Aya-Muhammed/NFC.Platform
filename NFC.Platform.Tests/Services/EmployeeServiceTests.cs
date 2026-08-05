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

            _messageService.Get(Arg.Any<string>()).Returns(x => (string)x[0]);

            _sut = new EmployeeService(_unitOfWork, _mapper, _messageService, _currentTenant, Substitute.For<IExcelParser>(), Substitute.For<System.Net.Http.IHttpClientFactory>());
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            _currentTenant.TenantId.Returns((Guid?)null);

            var request = new CreateEmployeeRequest { Email = "test@test.com" };

            var result = await _sut.CreateEmployeeAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsFail_WhenCompanyNotFound()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var queryableCompany = new List<Company>().BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var request = new CreateEmployeeRequest { Email = "test@test.com" };
            _messageService.Get("CompanyNotFound").Returns("Company not found for this tenant.");

            var result = await _sut.CreateEmployeeAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal("Company not found for this tenant.", result.Message);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ReturnsFail_WhenSubscriptionExpiredOrMissing()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid() };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var queryableSub = new List<UserSubscription>().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            var request = new CreateEmployeeRequest { Email = "test@test.com" };

            var result = await _sut.CreateEmployeeAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal("SubscriptionExpiredOrMissing", result.Message);
        }



        [Fact]
        public async Task CreateEmployeeAsync_ReturnsFail_WhenEmployeeAlreadyExistsWithEmail()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid() };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var plan = new SubscriptionPlan();
            var subscription = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10), SubscriptionPlan = plan };
            var queryableSub = new List<UserSubscription> { subscription }.BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(1);

            var existingEmployee = new Employee { Email = "duplicate@onpoint.com", TenantId = tenantId };
            _employeeRepo.GetQueryable().Returns(new List<Employee> { existingEmployee }.AsQueryable().BuildMock());

            _messageService.Get("UserAlreadyExists").Returns("User already exists.");

            var request = new CreateEmployeeRequest { Email = "duplicate@onpoint.com" };

            var result = await _sut.CreateEmployeeAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User already exists.", result.Message);
        }

        [Fact]
        public async Task CreateEmployeeAsync_Success_CreatesEmployeeAndProfile()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid(), Name = "OnPoint" };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var plan = new SubscriptionPlan();
            var subscription = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10), SubscriptionPlan = plan };
            var queryableSub = new List<UserSubscription> { subscription }.BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(5);
            _employeeRepo.GetQueryable().Returns(new List<Employee>().AsQueryable().BuildMock());

            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());

            var request = new CreateEmployeeRequest
            {
                Email = "new@onpoint.com",
                FullName = "New Employee",
                JobTitle = "Engineer",
                Department = "IT",
                ProfilePictureUrl = "http://test.com/pic.jpg",
                Phone = "+965 1234 5678",
                WhatsApp = "+965 8765 4321",
                Links = new List<NFC.Platform.Application.DTOs.Profile.CustomLinkInput>
                {
                    new NFC.Platform.Application.DTOs.Profile.CustomLinkInput { Title = "LinkedIn", Url = "https://linkedin.com/new" },
                    new NFC.Platform.Application.DTOs.Profile.CustomLinkInput { Title = "GitHub", Url = "https://github.com/new" }
                }
            };

            var mappedDto = new EmployeeDetailsDto { FullName = "New Employee" };
            _mapper.Map<Employee>(request).Returns(new Employee
            {
                FullName = request.FullName,
                Email = request.Email,
                JobTitle = request.JobTitle,
                Department = request.Department
            });
            _mapper.Map<UserProfile>(request).Returns(new UserProfile
            {
                FullName = request.FullName,
                JobTitle = request.JobTitle,
                Department = request.Department,
                ProfilePictureUrl = request.ProfilePictureUrl,
                Phone = request.Phone,
                WhatsApp = request.WhatsApp,
                ContactEmail = request.Email,
                CustomLinks = new List<ProfileLink>()
            });
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<Employee>()).Returns(mappedDto);
            _messageService.Get("RecordCreated").Returns("Employee created.");

            var result = await _sut.CreateEmployeeAsync(request);

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
                p.CustomLinks.Count == 2 &&
                p.CustomLinks.Any(l => l.Title == "LinkedIn" && l.Url == "https://linkedin.com/new") &&
                p.CustomLinks.Any(l => l.Title == "GitHub" && l.Url == "https://github.com/new")));

            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithCloudinaryProfilePicture_MapsUrlCorrectlyToUserProfile()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid(), Name = "CloudCompany" };
            var queryableCompany = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryableCompany);

            var plan = new SubscriptionPlan();
            var subscription = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(30), SubscriptionPlan = plan };
            var queryableSub = new List<UserSubscription> { subscription }.BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(0);
            _employeeRepo.GetQueryable().Returns(new List<Employee>().AsQueryable().BuildMock());

            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());

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
            _mapper.Map<Employee>(request).Returns(new Employee
            {
                FullName = request.FullName,
                Email = request.Email,
                JobTitle = request.JobTitle,
                Department = request.Department
            });
            _mapper.Map<UserProfile>(request).Returns(new UserProfile
            {
                FullName = request.FullName,
                JobTitle = request.JobTitle,
                Department = request.Department,
                ProfilePictureUrl = request.ProfilePictureUrl,
                ContactEmail = request.Email
            });
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<Employee>()).Returns(mappedDto);
            _messageService.Get("RecordCreated").Returns("Employee created.");

            var result = await _sut.CreateEmployeeAsync(request);

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

            var result = await _sut.GetPagedEmployeesAsync(request, "Emp");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalCount);
        }

        [Fact]
        public async Task GetEmployeeDetailsAsync_ReturnsNotFound_WhenEmployeeDoesNotExist()
        {
            var queryable = new List<Employee>().BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var result = await _sut.GetEmployeeDetailsAsync(Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetEmployeeDetailsAsync_ReturnsSuccess_WithEmployeeDetails()
        {
            var id = Guid.NewGuid();
            var employee = new Employee { Id = id, FullName = "John Doe" };
            var queryable = new List<Employee> { employee }.BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);

            var expectedDto = new EmployeeDetailsDto { Id = id, FullName = "John Doe" };
            _mapper.Map<EmployeeDetailsDto>(employee).Returns(expectedDto);

            var result = await _sut.GetEmployeeDetailsAsync(id);

            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Data!.Id);
        }

        [Fact]
        public async Task UpdateEmployeeJobDetailsAsync_ReturnsNotFound_WhenEmployeeDoesNotExist()
        {
            var queryable = new List<Employee>().BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var request = new UpdateEmployeeRequest { Status = UserStatus.Suspended };

            var result = await _sut.UpdateEmployeeJobDetailsAsync(Guid.NewGuid(), request);

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateEmployeeJobDetailsAsync_Success_UpdatesEmployeeAndUserProfile()
        {
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
                FullName = "New Name",
                JobTitle = "New Title",
                Department = "New Dept",
                Status = UserStatus.Active
            };

            _mapper.Map<EmployeeDetailsDto>(employee).Returns(new EmployeeDetailsDto { JobTitle = "New Title" });
            _mapper.Map(request, employee).Returns(x =>
            {
                employee.FullName = request.FullName ?? employee.FullName;
                employee.JobTitle = request.JobTitle ?? string.Empty;
                employee.Department = request.Department ?? string.Empty;
                employee.Status = request.Status;
                return employee;
            });
            _messageService.Get("RecordUpdated").Returns("Updated successfully.");

            var result = await _sut.UpdateEmployeeJobDetailsAsync(id, request);

            Assert.True(result.IsSuccess);
            Assert.Equal("New Name", employee.FullName);
            Assert.Equal("New Title", employee.JobTitle);
            _mapper.Received(1).Map(request, employee.UserProfile);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task SoftDeleteEmployeeAsync_ReturnsNotFound_WhenEmployeeDoesNotExist()
        {
            _employeeRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Employee)null!);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var result = await _sut.SoftDeleteEmployeeAsync(Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task SoftDeleteEmployeeAsync_RemovesEmployeeRecord_WhenValid()
        {
            var id = Guid.NewGuid();
            var employee = new Employee { Id = id };
            _employeeRepo.GetByIdAsync(id).Returns(employee);
            _messageService.Get("RecordDeleted").Returns("Record deleted.");

            var result = await _sut.SoftDeleteEmployeeAsync(id);

            Assert.True(result.IsSuccess);
            _employeeRepo.Received(1).Remove(employee);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }


        [Fact]
        public async Task GetPagedEmployeesAsync_FiltersByPhoneAndSubdomain()
        {
            var emp1 = new Employee
            {
                Id = Guid.NewGuid(),
                FullName = "Ahmed Soliman",
                Email = "ahmed@test.com",
                UserProfile = new UserProfile { Phone = "+96590001111", Subdomain = "ahmed-soliman" }
            };
            var emp2 = new Employee
            {
                Id = Guid.NewGuid(),
                FullName = "Mona Ali",
                Email = "mona@test.com",
                UserProfile = new UserProfile { Phone = "+96590002222", Subdomain = "mona-ali" }
            };

            var queryable = new List<Employee> { emp1, emp2 }.AsQueryable().BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);
            _mapper.Map<EmployeeDto>(Arg.Any<Employee>()).Returns(x => new EmployeeDto { FullName = ((Employee)x[0]).FullName });

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetPagedEmployeesAsync(request, "90001111");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.TotalCount);
            Assert.Equal("Ahmed Soliman", result.Data.Items.First().FullName);
        }

        [Fact]
        public async Task GetPagedEmployeesAsync_HandlesNullUserProfileAndJobTitle_WithoutCrashing()
        {
            var empWithNulls = new Employee
            {
                Id = Guid.NewGuid(),
                FullName = "Null Emp",
                Email = "nullemp@test.com",
                JobTitle = null!,
                Department = null!,
                UserProfile = null!
            };

            var queryable = new List<Employee> { empWithNulls }.AsQueryable().BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);
            _mapper.Map<EmployeeDto>(Arg.Any<Employee>()).Returns(new EmployeeDto());

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetPagedEmployeesAsync(request, "NonExistentSearchTerm");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Data!.TotalCount);
        }

        [Fact]
        public async Task GetPagedEmployeesAsync_WhenSearchNullOrEmpty_ReturnsAllEmployees()
        {
            var emp1 = new Employee { Id = Guid.NewGuid(), FullName = "Emp 1", Email = "emp1@test.com" };
            var emp2 = new Employee { Id = Guid.NewGuid(), FullName = "Emp 2", Email = "emp2@test.com" };

            var queryable = new List<Employee> { emp1, emp2 }.AsQueryable().BuildMock();
            _employeeRepo.GetQueryable().Returns(queryable);
            _mapper.Map<EmployeeDto>(Arg.Any<Employee>()).Returns(new EmployeeDto());

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var resultWithNull = await _sut.GetPagedEmployeesAsync(request, null);
            var resultWithSpaces = await _sut.GetPagedEmployeesAsync(request, "   ");

            Assert.True(resultWithNull.IsSuccess);
            Assert.Equal(2, resultWithNull.Data!.TotalCount);
            Assert.True(resultWithSpaces.IsSuccess);
            Assert.Equal(2, resultWithSpaces.Data!.TotalCount);
        }
    }
}
