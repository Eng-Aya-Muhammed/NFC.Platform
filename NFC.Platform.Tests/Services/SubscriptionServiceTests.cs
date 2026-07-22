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

        private readonly SubscriptionService _sut;

        public SubscriptionServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _planRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();

            _unitOfWork.Repository<SubscriptionPlan>().Returns(_planRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);

            _sut = new SubscriptionService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        [Fact]
        public async Task GetPlansAsync_ReturnsAllPlans_WithTranslations()
        {
            // Arrange
            var plans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan { Id = Guid.NewGuid(), Name = "PremiumAnnual", Description = "PremiumDesc", DurationInDays = 365, Price = 699 },
                new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Premium3Years", Description = "PremiumDesc", DurationInDays = 1095, Price = 699 }
            };

            var queryable = plans.AsQueryable().BuildMock();
            _planRepo.GetQueryable().Returns(queryable);

            var dtos = new List<SubscriptionPlanDto>
            {
                new SubscriptionPlanDto { Name = "PremiumAnnual", Description = "PremiumDesc", DurationInDays = 365, Price = 699 },
                new SubscriptionPlanDto { Name = "Premium3Years", Description = "PremiumDesc", DurationInDays = 1095, Price = 699 }
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
            Assert.Equal("Premium Description", result.Data![0].Description);
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

            var plan = new SubscriptionPlan { Name = "PremiumAnnual" };
            var activeSub = new UserSubscription
            {
                TenantId = tenantId,
                SubscriptionPlan = plan,
                EndDate = DateTime.UtcNow.AddDays(10),
                IsActive = true
            };

            var queryable = new List<UserSubscription> { activeSub }.AsQueryable().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryable);

            var dto = new UserSubscriptionDto { PlanName = "PremiumAnnual", IsActive = true };
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

            var plan = new SubscriptionPlan { Name = "PremiumAnnual" };
            var history = new List<UserSubscription>
            {
                new UserSubscription { TenantId = tenantId, SubscriptionPlan = plan, IsActive = false }
            };

            var queryable = history.AsQueryable().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(queryable);

            var dtos = new List<UserSubscriptionDto>
            {
                new UserSubscriptionDto { PlanName = "PremiumAnnual", IsActive = false }
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

            var plan = new SubscriptionPlan { Id = planId, Name = "PremiumAnnual", DurationInDays = 365 };
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

            var plan = new SubscriptionPlan { Id = planId, Name = "PremiumAnnual", DurationInDays = 365 };
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

            var plan = new SubscriptionPlan { Id = planId, Name = "PremiumAnnual", DurationInDays = 365 };
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

            var plan = new SubscriptionPlan { Id = planId, Name = "PremiumAnnual", DurationInDays = 365 };
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

            var plan = new SubscriptionPlan { Id = planId, Name = "PremiumAnnual", DurationInDays = 365 };
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
    }
}
