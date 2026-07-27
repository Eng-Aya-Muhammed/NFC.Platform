namespace NFC.Platform.Tests.Services
{
    public class SubscriptionServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        private readonly IGenericRepository<SubscriptionPlan> _planRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;
        private readonly IGenericRepository<Tenant> _tenantRepo;

        private readonly SubscriptionService _sut;

        public SubscriptionServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _planRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            _tenantRepo = Substitute.For<IGenericRepository<Tenant>>();

            _unitOfWork.Repository<SubscriptionPlan>().Returns(_planRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);
            _unitOfWork.Repository<Tenant>().Returns(_tenantRepo);

            _sut = new SubscriptionService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        [Fact]
        public async Task GetPlansAsync_ReturnsAllPlans_WithTranslations()
        {
            // Arrange
            var plans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan { Id = Guid.NewGuid(), NameAr = "PremiumAnnualAr", NameEn = "PremiumAnnualEn", Description = "PremiumDesc", DurationInDays = 365, Price = 699 },
                new SubscriptionPlan { Id = Guid.NewGuid(), NameAr = "Premium3YearsAr", NameEn = "Premium3YearsEn", Description = "PremiumDesc", DurationInDays = 1095, Price = 699 }
            };

            var queryable = plans.AsQueryable().BuildMock();
            _planRepo.GetQueryable().Returns(queryable);

            var dtos = new List<SubscriptionPlanDto>
            {
                new SubscriptionPlanDto { Name = "Premium - Annual", Description = "PremiumDesc", DurationInDays = 365, Price = 699 },
                new SubscriptionPlanDto { Name = "Premium - 3 Years", Description = "PremiumDesc", DurationInDays = 1095, Price = 699 }
            };

            _mapper.Map<IReadOnlyList<SubscriptionPlanDto>>(Arg.Any<List<SubscriptionPlan>>()).Returns(dtos);

            _messageService.Get("PremiumAnnual").Returns("Premium - Annual");
            _messageService.Get("Premium3Years").Returns("Premium - 3 Years");
            _messageService.Get("PremiumDesc").Returns("Premium Description");

            // Act
            var result = await _sut.GetPlansAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal("Premium - Annual", result.Data![0].Name);
            Assert.Equal("PremiumDesc", result.Data![0].Description);
        }

        [Fact]
        public async Task GetCurrentSubscriptionAsync_ReturnsUnauthorized_WhenTenantIdIsNull()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            // Act
            var result = await _sut.GetCurrentSubscriptionAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task GetCurrentSubscriptionAsync_ReturnsNotFound_WhenNoActiveSubscriptionExists()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var queryable = new List<UserSubscription>().AsQueryable().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryable);

            _messageService.Get("SubscriptionExpiredOrMissing").Returns("No active subscription found.");

            // Act
            var result = await _sut.GetCurrentSubscriptionAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("No active subscription found.", result.Message);
        }

        [Fact]
        public async Task GetCurrentSubscriptionAsync_ReturnsActiveSubscription_WithTranslations()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var plan = new SubscriptionPlan { NameAr = "PremiumAnnualAr", NameEn = "PremiumAnnualEn" };
            var activeSub = new UserSubscription
            {
                TenantId = tenantId,
                SubscriptionPlan = plan,
                EndDate = DateTime.UtcNow.AddDays(10),
                IsActive = true
            };

            var queryable = new List<UserSubscription> { activeSub }.AsQueryable().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryable);

            var dto = new UserSubscriptionDto { PlanName = "البريميوم - سنوي", IsActive = true };
            _mapper.Map<UserSubscriptionDto>(activeSub).Returns(dto);

            _messageService.Get("PremiumAnnual").Returns("البريميوم - سنوي");

            // Act
            var result = await _sut.GetCurrentSubscriptionAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("البريميوم - سنوي", result.Data!.PlanName);
        }

        [Fact]
        public async Task GetSubscriptionHistoryAsync_ReturnsHistory_WithTranslations()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var plan = new SubscriptionPlan { NameAr = "PremiumAnnualAr", NameEn = "PremiumAnnualEn" };
            var history = new List<UserSubscription>
            {
                new UserSubscription { TenantId = tenantId, SubscriptionPlan = plan, IsActive = false }
            };

            var queryable = history.AsQueryable().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryable);

            var dtos = new List<UserSubscriptionDto>
            {
                new UserSubscriptionDto { PlanName = "Premium - Annual", IsActive = false }
            };
            _mapper.Map<IReadOnlyList<UserSubscriptionDto>>(Arg.Any<List<UserSubscription>>()).Returns(dtos);

            _messageService.Get("PremiumAnnual").Returns("Premium - Annual");

            // Act
            var result = await _sut.GetSubscriptionHistoryAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
            Assert.Equal("Premium - Annual", result.Data![0].PlanName);
        }

        [Fact]
        public async Task RenewSubscriptionAsync_ReturnsUnauthorized_WhenTenantOrUserIdIsNull()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);
            _currentTenant.UserId.Returns((Guid?)null);

            var request = new RenewSubscriptionRequest { SubscriptionPlanId = Guid.NewGuid() };

            // Act
            var result = await _sut.RenewSubscriptionAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task RenewSubscriptionAsync_ReturnsNotFound_WhenPlanDoesNotExist()
        {
            // Arrange
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _currentTenant.UserId.Returns(Guid.NewGuid());

            _planRepo.GetQueryable().Returns(new List<SubscriptionPlan>().AsQueryable().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Plan not found.");

            var request = new RenewSubscriptionRequest { SubscriptionPlanId = Guid.NewGuid() };

            // Act
            var result = await _sut.RenewSubscriptionAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task SubscribeAsync_CreatesNewSubscription_WhenNoActiveSubscriptionExists()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentTenant.TenantId.Returns(tenantId);
            _currentTenant.UserId.Returns(userId);

            var plan = new SubscriptionPlan { Id = planId, NameAr = "PremiumAnnualAr", NameEn = "PremiumAnnualEn", DurationInDays = 365 };
            _planRepo.GetQueryable().Returns(new List<SubscriptionPlan> { plan }.AsQueryable().BuildMock());

            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription>().AsQueryable().BuildMock());

            var request = new SubscribeRequest { SubscriptionPlanId = planId };
            _mapper.Map<UserSubscription>(request).Returns(new UserSubscription { SubscriptionPlanId = planId });

            var dto = new UserSubscriptionDto { PlanName = "PremiumAnnual", IsActive = true };
            _mapper.Map<UserSubscriptionDto>(Arg.Any<UserSubscription>()).Returns(dto);

            _messageService.Get("PremiumAnnual").Returns("Premium - Annual");
            _messageService.Get("RecordCreated").Returns("Subscribed successfully.");

            // Act
            var result = await _sut.SubscribeAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            await _subscriptionRepo.Received(1).AddAsync(Arg.Is<UserSubscription>(s => 
                s.TenantId == Guid.Empty &&
                s.UserId == userId &&
                s.SubscriptionPlanId == planId &&
                s.StartDate <= DateTime.UtcNow &&
                s.EndDate > DateTime.UtcNow.AddDays(364) &&
                s.IsActive
            ));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task SubscribeAsync_ReturnsBadRequest_WhenActiveSubscriptionExists()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentTenant.TenantId.Returns(tenantId);
            _currentTenant.UserId.Returns(userId);

            var plan = new SubscriptionPlan { Id = planId, NameAr = "PremiumAnnualAr", NameEn = "PremiumAnnualEn", DurationInDays = 365 };
            _planRepo.GetQueryable().Returns(new List<SubscriptionPlan> { plan }.AsQueryable().BuildMock());

            var activeSub = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10) };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { activeSub }.AsQueryable().BuildMock());

            var request = new SubscribeRequest { SubscriptionPlanId = planId };
            _messageService.Get("HasActiveSubscription").Returns("You already have an active subscription.");

            // Act
            var result = await _sut.SubscribeAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("You already have an active subscription.", result.Message);
        }

        [Fact]
        public async Task RenewSubscriptionAsync_ExtendsExistingSubscription_WhenActiveSubscriptionExists()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentTenant.TenantId.Returns(tenantId);
            _currentTenant.UserId.Returns(userId);

            var plan = new SubscriptionPlan { Id = planId, NameAr = "PremiumAnnualAr", NameEn = "PremiumAnnualEn", DurationInDays = 365 };
            _planRepo.GetQueryable().Returns(new List<SubscriptionPlan> { plan }.AsQueryable().BuildMock());

            var activeSubEndDate = DateTime.UtcNow.AddDays(10);
            var activeSub = new UserSubscription
            {
                TenantId = tenantId,
                EndDate = activeSubEndDate,
                IsActive = true
            };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { activeSub }.AsQueryable().BuildMock());

            var request = new RenewSubscriptionRequest { SubscriptionPlanId = planId };
            _mapper.Map<UserSubscription>(request).Returns(new UserSubscription { SubscriptionPlanId = planId });

            var dto = new UserSubscriptionDto { PlanName = "PremiumAnnual", IsActive = true };
            _mapper.Map<UserSubscriptionDto>(Arg.Any<UserSubscription>()).Returns(dto);

            _messageService.Get("PremiumAnnual").Returns("Premium - Annual");

            // Act
            var result = await _sut.RenewSubscriptionAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            await _subscriptionRepo.Received(1).AddAsync(Arg.Is<UserSubscription>(s => 
                s.TenantId == Guid.Empty &&
                s.UserId == userId &&
                s.SubscriptionPlanId == planId &&
                s.StartDate == activeSubEndDate &&
                s.EndDate == activeSubEndDate.AddDays(365) &&
                s.IsActive
            ));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task RenewSubscriptionAsync_ReturnsBadRequest_WhenNoActiveSubscriptionExists()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentTenant.TenantId.Returns(tenantId);
            _currentTenant.UserId.Returns(userId);

            var plan = new SubscriptionPlan { Id = planId, NameAr = "PremiumAnnualAr", NameEn = "PremiumAnnualEn", DurationInDays = 365 };
            _planRepo.GetQueryable().Returns(new List<SubscriptionPlan> { plan }.AsQueryable().BuildMock());

            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription>().AsQueryable().BuildMock());

            var request = new RenewSubscriptionRequest { SubscriptionPlanId = planId };
            _messageService.Get("NoActiveSubscriptionToRenew").Returns("No active subscription found to renew.");

            // Act
            var result = await _sut.RenewSubscriptionAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("No active subscription found to renew.", result.Message);
        }

        [Fact]
        public async Task RenewSubscriptionAsync_StartsFromCurrentEndDate()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentTenant.TenantId.Returns(tenantId);
            _currentTenant.UserId.Returns(userId);

            var plan = new SubscriptionPlan { Id = planId, NameAr = "PremiumAnnualAr", NameEn = "PremiumAnnualEn", DurationInDays = 365 };
            _planRepo.GetQueryable().Returns(new List<SubscriptionPlan> { plan }.AsQueryable().BuildMock());

            var currentEndDate = DateTime.UtcNow.AddDays(30);
            var activeSub = new UserSubscription { TenantId = tenantId, IsActive = true, EndDate = currentEndDate };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { activeSub }.AsQueryable().BuildMock());

            var request = new RenewSubscriptionRequest { SubscriptionPlanId = planId };
            _mapper.Map<UserSubscription>(request).Returns(new UserSubscription());
            _mapper.Map<UserSubscriptionDto>(Arg.Any<UserSubscription>()).Returns(new UserSubscriptionDto { PlanName = "PremiumAnnual" });

            // Act
            var result = await _sut.RenewSubscriptionAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            await _subscriptionRepo.Received(1).AddAsync(Arg.Is<UserSubscription>(s => 
                s.StartDate == currentEndDate && 
                s.EndDate == currentEndDate.AddDays(365) &&
                s.IsActive == true));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task GetSubscriptionHistoryAsync_ReturnsEmpty_WhenNoHistory()
        {
            // Arrange
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription>().AsQueryable().BuildMock());
            _mapper.Map<IReadOnlyList<UserSubscriptionDto>>(Arg.Any<List<UserSubscription>>()).Returns(new List<UserSubscriptionDto>());

            // Act
            var result = await _sut.GetSubscriptionHistoryAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetSubscriptionHistoryAsync_ReturnsUnauthorized_WhenTenantIdIsNull()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            // Act
            var result = await _sut.GetSubscriptionHistoryAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task AdminExtendSubscriptionAsync_ReturnsNotFound_WhenTenantDoesNotExist()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _tenantRepo.GetQueryable().Returns(new List<Tenant>().AsQueryable().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var request = new NFC.Platform.Application.DTOs.Subscription.ExtendSubscriptionRequest { ExtensionDays = 30 };

            // Act
            var result = await _sut.AdminExtendSubscriptionAsync(tenantId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task AdminExtendSubscriptionAsync_ExtendsEndDate_PreservingStartDateAndQuotas_WhenActiveSubscriptionExists()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant { Id = tenantId, Name = "Acme Corp" };
            _tenantRepo.GetQueryable().Returns(new List<Tenant> { tenant }.AsQueryable().BuildMock());

            var originalStartDate = DateTime.UtcNow.AddDays(-10);
            var originalEndDate = DateTime.UtcNow.AddDays(20);
            var sub = new UserSubscription
            {
                TenantId = tenantId,
                StartDate = originalStartDate,
                EndDate = originalEndDate,
                IsActive = true,
                TemplateChangesUsed = 3,
                CustomDesignRequestsUsed = 1
            };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { sub }.AsQueryable().BuildMock());
            _mapper.Map<UserSubscriptionDto>(sub).Returns(new UserSubscriptionDto { Id = sub.Id });
            _messageService.Get("SubscriptionExtendedSuccessfully").Returns("Subscription extended successfully.");

            var request = new NFC.Platform.Application.DTOs.Subscription.ExtendSubscriptionRequest { ExtensionDays = 30 };

            // Act
            var result = await _sut.AdminExtendSubscriptionAsync(tenantId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(originalStartDate, sub.StartDate); // StartDate preserved for active subscription
            Assert.Equal(originalEndDate.AddDays(30), sub.EndDate); // EndDate extended from current EndDate
            Assert.Equal(3, sub.TemplateChangesUsed); // Quota preserved
            Assert.Equal(1, sub.CustomDesignRequestsUsed); // Quota preserved
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AdminExtendSubscriptionAsync_ReactivatesAndUpdatesStartDateToUtcNow_WhenSubscriptionExpired()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant { Id = tenantId, Name = "Acme Corp" };
            _tenantRepo.GetQueryable().Returns(new List<Tenant> { tenant }.AsQueryable().BuildMock());

            var expiredEndDate = DateTime.UtcNow.AddDays(-5);
            var sub = new UserSubscription
            {
                TenantId = tenantId,
                StartDate = DateTime.UtcNow.AddDays(-35),
                EndDate = expiredEndDate,
                IsActive = false
            };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { sub }.AsQueryable().BuildMock());
            _mapper.Map<UserSubscriptionDto>(sub).Returns(new UserSubscriptionDto { Id = sub.Id });
            _messageService.Get("SubscriptionExtendedSuccessfully").Returns("Subscription extended successfully.");

            var request = new NFC.Platform.Application.DTOs.Subscription.ExtendSubscriptionRequest { ExtensionDays = 60 };

            // Act
            var result = await _sut.AdminExtendSubscriptionAsync(tenantId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(sub.IsActive);
            Assert.True(sub.StartDate > expiredEndDate); // StartDate updated to UtcNow
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AdminExtendSubscriptionAsync_ReturnsBadRequest_WhenNoSubscriptionExistsToExtend()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant { Id = tenantId, Name = "Tenant Without Sub" };
            _tenantRepo.GetQueryable().Returns(new List<Tenant> { tenant }.AsQueryable().BuildMock());
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription>().AsQueryable().BuildMock());
            _messageService.Get("NoSubscriptionFoundToExtend").Returns("Cannot extend subscription for a customer with no prior subscription.");

            var request = new NFC.Platform.Application.DTOs.Subscription.ExtendSubscriptionRequest { ExtensionDays = 30 };

            // Act
            var result = await _sut.AdminExtendSubscriptionAsync(tenantId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Cannot extend subscription for a customer with no prior subscription.", result.Message);
        }

        [Fact]
        public async Task AdminExtendSubscriptionAsync_ReturnsBadRequest_WhenDaysIsZeroOrNegative()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _messageService.Get("InvalidExtensionDays").Returns("Invalid extension days.");

            var request = new NFC.Platform.Application.DTOs.Subscription.ExtendSubscriptionRequest { ExtensionDays = 0 };

            // Act
            var result = await _sut.AdminExtendSubscriptionAsync(tenantId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }
    }
}
