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
        private readonly IGenericRepository<CardTemplate> _cardTemplateRepo;

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
            _cardTemplateRepo = Substitute.For<IGenericRepository<CardTemplate>>();

            _unitOfWork.Repository<Company>().Returns(_companyRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);
            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<Employee>().Returns(_employeeRepo);
            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<ProfileMetric>().Returns(_metricRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);
            _unitOfWork.Repository<CardTemplate>().Returns(_cardTemplateRepo);

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
            Assert.Equal(15, result.Data!.SubscriptionRemainingDays);
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
            Assert.Equal("New OnPoint", result.Data!.Name);
            Assert.Equal("9999", adminUser.PhoneNumber);
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
                new() { UserProfileId = profileId1, UserProfile = new UserProfile { FullName = "Sara" }, CreatedAt = DateTime.UtcNow },
                new() { UserProfileId = profileId1, UserProfile = new UserProfile { FullName = "Sara" }, CreatedAt = DateTime.UtcNow },
                new() { UserProfileId = profileId2, UserProfile = new UserProfile { FullName = "John" }, CreatedAt = DateTime.UtcNow }
            };
            _metricRepo.GetQueryable().Returns(metrics.AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetCompanyDashboardAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(10, result.Data!.TotalEmployeesCount);
            Assert.Equal(5, result.Data!.CardRequestsCount);
            Assert.Equal(100, result.Data!.ContactSavesCount);
            Assert.Equal("Sara", result.Data!.TopEmployeeName);
            Assert.Equal(6, result.Data!.MonthlyMetrics.Count);
        }

        [Fact]
        public async Task GetMyCompanyProfileAsync_ReturnsZeroRemainingDays_WhenNoActiveSubscription()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Name = "OnPoint", TenantId = tenantId };
            _companyRepo.GetQueryable().Returns(new List<Company> { company }.BuildMock());

            // No active subscription
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription>().BuildMock());

            var dto = new CompanyProfileDto { Name = "OnPoint" };
            _mapper.Map<CompanyProfileDto>(company).Returns(dto);

            // Act
            var result = await _sut.GetMyCompanyProfileAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Data!.SubscriptionRemainingDays);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            // Act
            var result = await _sut.UpdateCompanyProfileAsync(new UpdateCompanyProfileRequest());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_ReturnsNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _companyRepo.GetQueryable().Returns(new List<Company>().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.UpdateCompanyProfileAsync(new UpdateCompanyProfileRequest { Name = "X" });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_DoesNotUpdatePhone_WhenAdminUserIsNull()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Name = "Old", TenantId = tenantId, AdminUser = null! };
            _companyRepo.GetQueryable().Returns(new List<Company> { company }.BuildMock());
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription>().BuildMock());

            var request = new UpdateCompanyProfileRequest { Name = "New", Phone = "999" };
            _mapper.Map(request, company).Returns(_ => company);

            var dto = new CompanyProfileDto { Name = "New" };
            _mapper.Map<CompanyProfileDto>(company).Returns(dto);
            _messageService.Get("RecordUpdated").Returns("Updated.");

            // Act
            var result = await _sut.UpdateCompanyProfileAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            _userRepo.DidNotReceive().Update(Arg.Any<User>());
        }

        [Fact]
        public async Task ChangeCompanyAdminPasswordAsync_ReturnsUnauthorized_WhenUserIdMissing()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);

            // Act
            var result = await _sut.ChangeCompanyAdminPasswordAsync(new CompanyChangePasswordRequest());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task ChangeCompanyAdminPasswordAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _userRepo.GetByIdAsync(userId).Returns((User?)null);
            _messageService.Get("RecordNotFound").Returns("Not found.");

            // Act
            var result = await _sut.ChangeCompanyAdminPasswordAsync(new CompanyChangePasswordRequest
            {
                OldPassword = "any",
                NewPassword = "any2"
            });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ChangeCompanyAdminPasswordAsync_ReturnsSuccess_WhenPasswordIsCorrect()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var user = new User
            {
                Id = userId,
                PasswordHash = NFC.Platform.BuildingBlocks.Common.Helpers.PasswordHasher.HashPassword("OldPass123!")
            };
            _userRepo.GetByIdAsync(userId).Returns(user);
            _messageService.Get("PasswordResetSuccess").Returns("Password changed.");

            // Act
            var result = await _sut.ChangeCompanyAdminPasswordAsync(new CompanyChangePasswordRequest
            {
                OldPassword = "OldPass123!",
                NewPassword = "NewPass456!"
            });

            // Assert
            Assert.True(result.IsSuccess);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task GetCompanyDashboardAsync_ReturnsDashboard_WithFallbackTopEmployee_WhenNoMetrics()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            _employeeRepo.CountAsync().Returns(0);
            _orderRepo.CountAsync().Returns(0);
            _metricRepo.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ProfileMetric, bool>>>()).Returns(0);

            // No metrics → top employee = "-"
            _metricRepo.GetQueryable().Returns(new List<ProfileMetric>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetCompanyDashboardAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("-", result.Data!.TopEmployeeName);
            Assert.Equal(6, result.Data!.MonthlyMetrics.Count);
        }

        [Fact]
        public async Task GetCompanyDashboardAsync_ReturnsUnauthorized_WhenTenantIdIsNull()
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
        public async Task GetCompanyDashboardAsync_ReturnsCorrectDashboardCounts_WhenMetricsExist()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            _employeeRepo.CountAsync().Returns(5);
            _orderRepo.CountAsync().Returns(3);
            _metricRepo.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ProfileMetric, bool>>>()).Returns(15);

            var profileMetrics = new List<ProfileMetric>
            {
                new ProfileMetric { UserProfile = new UserProfile { FullName = "Top Employee" }, CreatedAt = DateTime.UtcNow }
            };
            _metricRepo.GetQueryable().Returns(profileMetrics.AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetCompanyDashboardAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Data!.TotalEmployeesCount);
            Assert.Equal(3, result.Data!.CardRequestsCount);
            Assert.Equal(15, result.Data!.ContactSavesCount);
            Assert.Equal("Top Employee", result.Data!.TopEmployeeName);
        }

        [Fact]
        public async Task ChangeCompanyAdminPasswordAsync_ReturnsFail_WhenOldPasswordIsIncorrect()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var user = new User
            {
                Id = userId,
                PasswordHash = NFC.Platform.BuildingBlocks.Common.Helpers.PasswordHasher.HashPassword("CorrectOldPass123!")
            };
            _userRepo.GetByIdAsync(userId).Returns(user);

            // Act
            var result = await _sut.ChangeCompanyAdminPasswordAsync(new CompanyChangePasswordRequest
            {
                OldPassword = "IncorrectOldPass!",
                NewPassword = "NewPassword123!"
            });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyTemplateAsync_ReturnsUnauthorized_WhenTenantIdIsNull()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);
            var request = Guid.NewGuid();

            // Act
            var result = await _sut.UpdateCompanyTemplateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyTemplateAsync_ReturnsNotFound_WhenCompanyDoesNotExist()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            _companyRepo.GetQueryable().Returns(new List<Company>().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Record not found.");
            var request = Guid.NewGuid();

            // Act
            var result = await _sut.UpdateCompanyTemplateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyTemplateAsync_ReturnsSuccess_AndSavesChanges()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var company = new Company { Id = Guid.NewGuid(), Name = "OnPoint", TenantId = tenantId };
            var queryable = new List<Company> { company }.BuildMock();
            _companyRepo.GetQueryable().Returns(queryable);

            var templateId = Guid.NewGuid();
            var request = templateId;

            // Mock CardTemplate repository
            var template = new CardTemplate { Id = templateId, IsActive = true, IsDeleted = false };
            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate> { template }.AsQueryable().BuildMock());

            // Mock subscription repository to prevent EF async query provider error in GetSubscriptionRemainingDaysAsync
            var sub = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30),
                TemplateChangesUsed = 1,
                SubscriptionPlan = new SubscriptionPlan
                {
                    MaxTemplateChanges = 5,
                    PlanTemplates = new List<SubscriptionPlanTemplate> { new() { CardTemplateId = templateId } }
                }
            };
            var queryableSub = new List<UserSubscription> { sub }.AsQueryable().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryableSub);

            var dto = new CompanyProfileDto { Id = company.Id, Name = "OnPoint", ProfileTemplateId = templateId };

            _mapper.Map<CompanyProfileDto>(company).Returns(dto);
            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            // Act
            var result = await _sut.UpdateCompanyTemplateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(templateId, company.ProfileTemplateId);
            Assert.Equal(2, sub.TemplateChangesUsed); // Incremented
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
