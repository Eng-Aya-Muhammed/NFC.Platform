namespace NFC.Platform.Tests.Services
{
    public class AdminServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        private readonly IStorageService _storageService;

        private readonly IGenericRepository<CardOrder> _orderRepo;
        private readonly IGenericRepository<TemplateRequest> _templateRequestRepo;
        private readonly IGenericRepository<CardTemplate> _cardTemplateRepo;
        private readonly IGenericRepository<Tenant> _tenantRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;
        private readonly IGenericRepository<CardPricing> _cardPricingRepo;
        private readonly IGenericRepository<Company> _companyRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;
        private readonly IBackgroundJobClient _backgroundJobClient;

        private readonly AdminService _sut;

        public AdminServiceTests()
        {
            _unitOfWork          = Substitute.For<IUnitOfWork>();
            _mapper              = Substitute.For<IMapper>();
            _messageService      = Substitute.For<IMessageService>();

            _storageService      = Substitute.For<IStorageService>();
            _backgroundJobClient = Substitute.For<IBackgroundJobClient>();

            _orderRepo           = Substitute.For<IGenericRepository<CardOrder>>();
            _templateRequestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();
            _cardTemplateRepo    = Substitute.For<IGenericRepository<CardTemplate>>();
            _tenantRepo          = Substitute.For<IGenericRepository<Tenant>>();
            _subscriptionRepo    = Substitute.For<IGenericRepository<UserSubscription>>();
            _cardPricingRepo     = Substitute.For<IGenericRepository<CardPricing>>();
            _companyRepo         = Substitute.For<IGenericRepository<Company>>();
            _userProfileRepo     = Substitute.For<IGenericRepository<UserProfile>>();

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<TemplateRequest>().Returns(_templateRequestRepo);
            _unitOfWork.Repository<CardTemplate>().Returns(_cardTemplateRepo);
            _unitOfWork.Repository<Tenant>().Returns(_tenantRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);
            _unitOfWork.Repository<CardPricing>().Returns(_cardPricingRepo);
            _unitOfWork.Repository<Company>().Returns(_companyRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);

            // Seed mock queryable lists to avoid EF FirstOrDefaultAsync failures in tests
            _companyRepo.GetQueryable().Returns(new List<Company>().AsQueryable().BuildMock());
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());



            _storageService
                .UploadBytesAsImageAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto
                {
                    SecureUrl = "https://res.cloudinary.com/demo/image/upload/qr-placeholder.png",
                    PublicId  = "nfc-platform/qrcodes/test/qr-placeholder"
                }));

            _sut = new AdminService(_unitOfWork, _mapper, _messageService, _storageService, _backgroundJobClient);
        }

        // ── GetOrdersPagedAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetOrdersPagedAsync_ReturnsAllOrders_WhenNoStatusFilterPassed()
        {
            // Arrange
            var orders = new List<CardOrder>
            {
                new() { Id = Guid.NewGuid(), CardName = "Order 1", Status = OrderStatus.PendingReview },
                new() { Id = Guid.NewGuid(), CardName = "Order 2", Status = OrderStatus.UnderReview }
            };
            var mockQueryable = orders.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            _mapper.Map<AdminOrderSummaryDto>(Arg.Any<CardOrder>())
                .Returns(x => new AdminOrderSummaryDto { CardName = ((CardOrder)x[0]).CardName });

            // Act
            var result = await _sut.GetOrdersPagedAsync(pagination, null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Items.Count);
        }

        [Fact]
        public async Task GetOrdersPagedAsync_FiltersOrders_WhenStatusFilterPassed()
        {
            // Arrange
            var orders = new List<CardOrder>
            {
                new() { Id = Guid.NewGuid(), CardName = "Order 1", Status = OrderStatus.PendingReview },
                new() { Id = Guid.NewGuid(), CardName = "Order 2", Status = OrderStatus.UnderReview }
            };
            var mockQueryable = orders.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            _mapper.Map<AdminOrderSummaryDto>(Arg.Any<CardOrder>())
                .Returns(x => new AdminOrderSummaryDto { CardName = ((CardOrder)x[0]).CardName, Status = ((CardOrder)x[0]).Status });

            // Act
            var result = await _sut.GetOrdersPagedAsync(pagination, OrderStatus.UnderReview);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!.Items);
            Assert.Equal(OrderStatus.UnderReview, result.Data.Items.First().Status);
        }

        // ── GetOrderByIdAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetOrderByIdAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var mockQueryable = new List<CardOrder>().AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            // Act
            var result = await _sut.GetOrderByIdAsync(Guid.NewGuid());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetOrderByIdAsync_ReturnsOrderDetail_WhenOrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var orders = new List<CardOrder>
            {
                new() { Id = orderId, CardName = "Special Order" }
            };
            var mockQueryable = orders.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var dto = new AdminOrderDetailDto { Id = orderId, CardName = "Special Order" };
            _mapper.Map<AdminOrderDetailDto>(Arg.Any<CardOrder>()).Returns(dto);

            // Act
            var result = await _sut.GetOrderByIdAsync(orderId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Special Order", result.Data!.CardName);
        }

        // ── UpdateOrderStatusAsync ────────────────────────────────────────────────

        [Fact]
        public async Task UpdateOrderStatusAsync_UpdatesStatusAndTracking_WhenOrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new CardOrder { Id = orderId, Status = OrderStatus.PendingReview };
            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var updateDto = new UpdateOrderStatusDto
            {
                Status = OrderStatus.ReadyForDelivery,
                TrackingNumber = "TRK12345"
            };

            // Act
            var result = await _sut.UpdateOrderStatusAsync(orderId, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(OrderStatus.ReadyForDelivery, order.Status);
            Assert.Equal("TRK12345", order.TrackingNumber);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── ResolveTemplateRequestAsync ───────────────────────────────────────────

        [Fact]
        public async Task ResolveTemplateRequestAsync_CreatesCustomTemplate_WhenApproved()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var request = new TemplateRequest
            {
                Id = requestId,
                TenantId = tenantId,
                TemplateName = "Corporate Template 1",
                ReferenceImageUrl = "url",
                Notes = "Original notes"
            };
            _templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest> { request }.AsQueryable().BuildMock());

            var resolveDto = new ResolveTemplateRequestDto
            {
                Status = TemplateRequestStatus.Completed,
                StyleConfigJson = "{\"color\": \"blue\"}",
                Notes = "Design complete"
            };

            // Act
            var result = await _sut.ResolveTemplateRequestAsync(requestId, resolveDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(TemplateRequestStatus.Completed, request.Status);
            Assert.Contains("Admin Notes: Design complete", request.Notes);

            await _cardTemplateRepo.Received(1).AddAsync(Arg.Is<CardTemplate>(t =>
                t.TenantId == tenantId &&
                t.Name == "Corporate Template 1" &&
                t.StyleConfigJson == "{\"color\": \"blue\"}"));

            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── CreateTemplateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateTemplateAsync_SavesTemplateAndReturnsDto()
        {
            // Arrange
            var createDto = new CreateCardTemplateDto
            {
                Name = "Modern Template",
                Category = "Professional",
                StyleConfigJson = "{}",
                ThumbnailUrl = "thumb.png"
            };

            var mappedTemplate = new CardTemplate
            {
                Name = "Modern Template",
                Category = "Professional",
                StyleConfigJson = "{}",
                ThumbnailUrl = "thumb.png"
            };

            _mapper.Map<CardTemplate>(createDto).Returns(mappedTemplate);
            _mapper.Map<CardTemplateDto>(mappedTemplate).Returns(new CardTemplateDto { Name = "Modern Template", PreviewImageUrl = "thumb.png" });

            // Act
            var result = await _sut.CreateTemplateAsync(createDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(mappedTemplate.TenantId);
            Assert.Equal("Modern Template", result.Data!.Name);
            await _cardTemplateRepo.Received(1).AddAsync(mappedTemplate);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── UpdateTemplateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateTemplateAsync_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            _cardTemplateRepo.GetByIdAsync(templateId).Returns((CardTemplate?)null);

            // Act
            var result = await _sut.UpdateTemplateAsync(templateId, new UpdateCardTemplateDto());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateTemplateAsync_UpdatesValues_WhenTemplateExists()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            var template = new CardTemplate { Id = templateId, Name = "Old Name" };
            _cardTemplateRepo.GetByIdAsync(templateId).Returns(template);

            var updateDto = new UpdateCardTemplateDto { Name = "New Name" };
            _mapper.Map(updateDto, template).Returns(template);
            _mapper.Map<CardTemplateDto>(template).Returns(new CardTemplateDto { Id = templateId, Name = "New Name" });

            // Act
            var result = await _sut.UpdateTemplateAsync(templateId, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("New Name", result.Data!.Name);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── DeleteTemplateAsync ───────────────────────────────────────────────────

        // ── GetTenantsPagedAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetTenantsPagedAsync_ReturnsPagedTenantsWithSubscriptions()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var tenants = new List<Tenant>
            {
                new() { Id = tenantId, Name = "ACME Corp", IsActive = true }
            };
            var mockQueryable = tenants.AsQueryable().BuildMock();
            _tenantRepo.GetQueryable().Returns(mockQueryable);

            var subscriptions = new List<UserSubscription>
            {
                new() { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(30), SubscriptionPlan = new SubscriptionPlan { Name = "Premium Plan" } }
            };
            var mockSubQuery = subscriptions.AsQueryable().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(mockSubQuery);

            _mapper.Map<TenantSummaryDto>(Arg.Any<Tenant>()).Returns(new TenantSummaryDto { Id = tenantId, Name = "ACME Corp", IsActive = true });

            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _sut.GetTenantsPagedAsync(pagination);

            // Assert
            Assert.True(result.IsSuccess);
            var item = result.Data!.Items.First();
            Assert.Equal("Premium Plan", item.ActivePlanName);
            Assert.True(item.DaysRemaining > 0);
        }

        // ── UpdateTenantStatusAsync ───────────────────────────────────────────────

        [Fact]
        public async Task UpdateTenantStatusAsync_TogglesTenantActiveState()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant { Id = tenantId, IsActive = true };
            _tenantRepo.GetByIdAsync(tenantId).Returns(tenant);

            var updateDto = new UpdateTenantStatusDto { IsActive = false };

            // Act
            var result = await _sut.UpdateTenantStatusAsync(tenantId, updateDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(tenant.IsActive);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── UpdateCardPricingAsync ────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("KW")]
        [InlineData("KWD1")]
        public async Task UpdateCardPricingAsync_ReturnsFailure_WhenCurrencyIsInvalid(string invalidCurrency)
        {
            // Arrange
            var dto = new UpdateCardPricingDto
            {
                CardType = CardType.Plastic,
                UnitPrice = 5.0m,
                Currency = invalidCurrency
            };

            // Act
            var result = await _sut.UpdateCardPricingAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCardPricingAsync_ClosesOutOldAndInsertsNewPricingRecord()
        {
            // Arrange
            var existingPricing = new CardPricing
            {
                Id = Guid.NewGuid(),
                CardType = CardType.Plastic,
                UnitPrice = 4.5m,
                Currency = "KWD",
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-10)
            };

            var pricingsList = new List<CardPricing> { existingPricing };
            var mockQueryable = pricingsList.AsQueryable().BuildMock();
            _cardPricingRepo.GetQueryable().Returns(mockQueryable);

            var dto = new UpdateCardPricingDto
            {
                CardType = CardType.Plastic,
                UnitPrice = 5.0m,
                Currency = "kwd " // Will be normalized to KWD
            };

            // Act
            var result = await _sut.UpdateCardPricingAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(existingPricing.IsActive);
            Assert.NotNull(existingPricing.EffectiveTo);
            await _cardPricingRepo.Received(1).AddAsync(Arg.Is<CardPricing>(p => 
                p.CardType == CardType.Plastic && 
                p.UnitPrice == 5.0m && 
                p.Currency == "KWD" && 
                p.IsActive == true && 
                p.EffectiveTo == null));

            await _unitOfWork.Received(1).SaveChangesAsync();
            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        [Fact]
        public async Task UpdateCardPricingAsync_ReturnsFail_WhenDtoIsNull()
        {
            // Act
            var result = await _sut.UpdateCardPricingAsync(null!);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCardPricingAsync_ReturnsFail_WhenCurrencyIsInvalid()
        {
            // Arrange
            var dto = new UpdateCardPricingDto { CardType = CardType.Plastic, UnitPrice = 5.0m, Currency = "US" }; // Not 3 chars

            // Act
            var result = await _sut.UpdateCardPricingAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCardPricingAsync_ReturnsSuccessNoOp_WhenPriceAndCurrencyMatch()
        {
            // Arrange
            var existingPricing = new CardPricing { CardType = CardType.Plastic, UnitPrice = 5.0m, Currency = "KWD", IsActive = true };
            _cardPricingRepo.GetQueryable().Returns(new List<CardPricing> { existingPricing }.AsQueryable().BuildMock());

            var dto = new UpdateCardPricingDto { CardType = CardType.Plastic, UnitPrice = 5.0m, Currency = "KWD" };

            // Act
            var result = await _sut.UpdateCardPricingAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(existingPricing.IsActive);
            await _cardPricingRepo.DidNotReceive().AddAsync(Arg.Any<CardPricing>());
            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        [Fact]
        public async Task CreateTemplateAsync_CreatesGlobalTemplate()
        {
            // Arrange
            var dto = new CreateCardTemplateDto { Name = "Global Temp", Category = "General" };
            var mappedTemplate = new CardTemplate { Name = "Global Temp", Category = "General" };
            _mapper.Map<CardTemplate>(dto).Returns(mappedTemplate);

            var expectedDto = new CardTemplateDto { Name = "Global Temp" };
            _mapper.Map<CardTemplateDto>(mappedTemplate).Returns(expectedDto);

            // Act
            var result = await _sut.CreateTemplateAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(mappedTemplate.TenantId);
            await _cardTemplateRepo.Received(1).AddAsync(mappedTemplate);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteTemplateAsync_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _cardTemplateRepo.GetByIdAsync(id).Returns((CardTemplate?)null);

            // Act
            var result = await _sut.DeleteTemplateAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.ResolveTemplateRequestAsync(id, new ResolveTemplateRequestDto());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_EnqueuesEmailNotification_WhenStatusCompleted()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var user = new User { Email = "client@example.com" };
            var templateRequest = new TemplateRequest
            {
                Id = requestId,
                TenantId = tenantId,
                TemplateName = "Golden Luxury",
                Status = TemplateRequestStatus.Pending,
                RequestedByUser = user
            };

            _templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock());
            _companyRepo.GetQueryable().Returns(new List<Company>().AsQueryable().BuildMock());
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());
            _messageService.Get("RecordUpdated").Returns("Record updated successfully.");

            var dto = new ResolveTemplateRequestDto
            {
                Status = TemplateRequestStatus.Completed,
                StyleConfigJson = "{\"color\":\"#ffd700\"}"
            };

            // Act
            var result = await _sut.ResolveTemplateRequestAsync(requestId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(TemplateRequestStatus.Completed, templateRequest.Status);
            Assert.NotNull(templateRequest.ProducedTemplateId);

            _backgroundJobClient.Received(1).Create(
                Arg.Is<Hangfire.Common.Job>(j =>
                    j.Method.Name == nameof(IEmailService.SendTemplateRequestApprovedEmailAsync)),
                Arg.Any<Hangfire.States.IState>());
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_DoesNotEnqueueEmail_WhenStatusNotCompleted()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var user = new User { Email = "client@example.com" };
            var templateRequest = new TemplateRequest
            {
                Id = requestId,
                TenantId = tenantId,
                TemplateName = "Rejected Template Request",
                Status = TemplateRequestStatus.Pending,
                RequestedByUser = user
            };

            _templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock());
            _companyRepo.GetQueryable().Returns(new List<Company>().AsQueryable().BuildMock());
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());
            _messageService.Get("RecordUpdated").Returns("Record updated successfully.");

            var dto = new ResolveTemplateRequestDto
            {
                Status = TemplateRequestStatus.Rejected,
                Notes = "Inappropriate design logo"
            };

            // Act
            var result = await _sut.ResolveTemplateRequestAsync(requestId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(TemplateRequestStatus.Rejected, templateRequest.Status);
            Assert.Null(templateRequest.ProducedTemplateId);

            // Verify email notification is NOT enqueued when rejected
            _backgroundJobClient.DidNotReceive().Create(
                Arg.Is<Hangfire.Common.Job>(j =>
                    j.Method.Name == nameof(IEmailService.SendTemplateRequestApprovedEmailAsync)),
                Arg.Any<Hangfire.States.IState>());
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_AppliesTemplateToCompanyProfile_WhenCompanyExists()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var company = new Company { Id = Guid.NewGuid(), TenantId = tenantId };
            var user = new User { Email = "admin@company.com" };

            var templateRequest = new TemplateRequest
            {
                Id = requestId,
                TenantId = tenantId,
                TemplateName = "Company Corporate Layout",
                Status = TemplateRequestStatus.Pending,
                RequestedByUser = user
            };

            _templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock());
            _companyRepo.GetQueryable().Returns(new List<Company> { company }.AsQueryable().BuildMock());
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());
            _messageService.Get("RecordUpdated").Returns("Record updated successfully.");

            var dto = new ResolveTemplateRequestDto
            {
                Status = TemplateRequestStatus.Completed,
                StyleConfigJson = "{\"theme\":\"navy\"}"
            };

            // Act
            var result = await _sut.ResolveTemplateRequestAsync(requestId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(templateRequest.ProducedTemplateId);
            Assert.Equal(templateRequest.ProducedTemplateId, company.ProfileTemplateId);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_AppliesTemplateToUserProfile_WhenCompanyDoesNotExist()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Email = "individual@example.com" };
            var userProfile = new UserProfile { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId };

            var templateRequest = new TemplateRequest
            {
                Id = requestId,
                TenantId = tenantId,
                RequestedByUserId = userId,
                TemplateName = "Personal Artist Layout",
                Status = TemplateRequestStatus.Pending,
                RequestedByUser = user
            };

            _templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock());
            _companyRepo.GetQueryable().Returns(new List<Company>().AsQueryable().BuildMock()); // No company
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile> { userProfile }.AsQueryable().BuildMock());
            _messageService.Get("RecordUpdated").Returns("Record updated successfully.");

            var dto = new ResolveTemplateRequestDto
            {
                Status = TemplateRequestStatus.Completed,
                StyleConfigJson = "{\"theme\":\"minimalist\"}"
            };

            // Act
            var result = await _sut.ResolveTemplateRequestAsync(requestId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(templateRequest.ProducedTemplateId);
            Assert.Equal(templateRequest.ProducedTemplateId, userProfile.ProfileTemplateId);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
        // ── ReassignSubdomainAsync ────────────────────────────────────────────────

        [Fact]
        public async Task ReassignSubdomainAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            _userProfileRepo.GetByIdAsync(profileId).Returns((UserProfile?)null);
            _messageService.Get("RecordNotFound").Returns("Profile not found.");

            // Act
            var result = await _sut.ReassignSubdomainAsync(profileId, "new-subdomain");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("Profile not found.", result.Message);
        }

        [Fact]
        public async Task ReassignSubdomainAsync_ReturnsConflict_WhenSubdomainAlreadyTaken()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            var profile = new UserProfile { Id = profileId, Subdomain = "old-subdomain" };
            _userProfileRepo.GetByIdAsync(profileId).Returns(profile);

            // Mock that another profile already has the normalized subdomain
            var anotherProfile = new UserProfile { Id = Guid.NewGuid(), Subdomain = "new-subdomain" };
            var queryable = new List<UserProfile> { anotherProfile }.AsQueryable().BuildMock();
            _userProfileRepo.GetQueryable().Returns(queryable);

            _messageService.Get("SubdomainAlreadyTaken").Returns("This subdomain is already taken.");

            // Act
            var result = await _sut.ReassignSubdomainAsync(profileId, "New Subdomain");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("This subdomain is already taken.", result.Message);
        }

        [Fact]
        public async Task ReassignSubdomainAsync_UpdatesSubdomain_WhenUnique()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            var profile = new UserProfile { Id = profileId, Subdomain = "old-subdomain" };
            _userProfileRepo.GetByIdAsync(profileId).Returns(profile);

            // Mock that NO other profile has the new subdomain
            var queryable = new List<UserProfile>().AsQueryable().BuildMock();
            _userProfileRepo.GetQueryable().Returns(queryable);

            // Act
            var result = await _sut.ReassignSubdomainAsync(profileId, "Fresh New Subdomain");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("fresh-new-subdomain", profile.Subdomain);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── OTP Expiration & Resend Unit Tests ────────────────────────────────────

        [Fact]
        public async Task VerifyDeliveryOtpAsync_ReturnsFail_WhenOtpHasExpired()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id                   = orderId,
                Status               = OrderStatus.ReadyForDelivery,
                DeliveryOtp          = "123456",
                DeliveryOtpExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired 5 mins ago
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpExpired").Returns("OTP code has expired.");

            // Act
            var result = await _sut.VerifyDeliveryOtpAsync(orderId, "123456");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal("OTP code has expired.", result.Message);
            Assert.Equal(OrderStatus.ReadyForDelivery, order.Status); // Status unchanged
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsFail_WhenCooldownActive()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id                   = orderId,
                Status               = OrderStatus.ReadyForDelivery,
                DeliveryOtp          = "123456",
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddSeconds(-30), // Sent 30 seconds ago (< 60s)
                DeliveryOtpResendCount = 1
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpCooldownActive").Returns("Please wait 60 seconds.");

            // Act
            var result = await _sut.ResendDeliveryOtpAsync(orderId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal("Please wait 60 seconds.", result.Message);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsFail_WhenResendLimitReached()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id                   = orderId,
                Status               = OrderStatus.ReadyForDelivery,
                DeliveryOtp          = "123456",
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddMinutes(-10),
                DeliveryOtpResendCount = 5 // Max limit reached (5)
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpResendLimitReached").Returns("Limit reached.");

            // Act
            var result = await _sut.ResendDeliveryOtpAsync(orderId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal("Limit reached.", result.Message);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_GeneratesNewOtp_ResetsExpiry_IncrementsCount_AndEnqueuesJobs()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var user = new User
            {
                Email = "customer@example.com",
                UserProfile = new UserProfile { WhatsApp = "+201013503890" }
            };
            var order = new CardOrder
            {
                Id                   = orderId,
                Status               = OrderStatus.ReadyForDelivery,
                CardName             = "My NFC Card",
                DeliveryOtp          = "111111",
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddMinutes(-2), // > 60s ago
                DeliveryOtpResendCount = 2,
                Tenant               = new Tenant { Company = null },
                User                 = user
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpResent").Returns("OTP code has been resent successfully.");

            // Act
            var result = await _sut.ResendDeliveryOtpAsync(orderId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("OTP code has been resent successfully.", result.Message);
            Assert.NotEqual("111111", order.DeliveryOtp); // New OTP generated
            Assert.Equal(6, order.DeliveryOtp!.Length);
            Assert.Equal(3, order.DeliveryOtpResendCount); // Incremented from 2 to 3
            Assert.NotNull(order.DeliveryOtpExpiresAt);
            Assert.True(order.DeliveryOtpExpiresAt > DateTime.UtcNow);

            await _unitOfWork.Received(1).SaveChangesAsync();

            // Background jobs enqueued
            _backgroundJobClient.Received(1).Create(
                Arg.Is<Hangfire.Common.Job>(j =>
                    j.Method.Name == nameof(IEmailService.SendOrderReadyOtpEmailAsync) &&
                    j.Args[0].ToString() == "customer@example.com"),
                Arg.Any<Hangfire.States.IState>());

            _backgroundJobClient.Received(1).Create(
                Arg.Is<Hangfire.Common.Job>(j =>
                    j.Method.Name == nameof(IWhatsAppService.SendWhatsAppMessageAsync) &&
                    j.Args[0].ToString() == "+201013503890"),
                Arg.Any<Hangfire.States.IState>());
        }

        // ── Plan Management ───────────────────────────────────────────────────────

        [Fact]
        public async Task CreatePlanAsync_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new CreateSubscriptionPlanRequest
            {
                Name = "Business",
                Description = "Business plan",
                Price = 199m,
                DurationInDays = 365,
                MaxEmployees = 50,
                MaxTemplateChanges = 5,
                MaxCustomDesignRequests = 2
            };

            var planRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();
            var planTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();

            _unitOfWork.Repository<SubscriptionPlan>().Returns(planRepo);
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(planTemplateRepo);

            var plan = new SubscriptionPlan { Id = Guid.NewGuid(), Name = request.Name };
            _mapper.Map<SubscriptionPlan>(request).Returns(plan);

            planRepo.GetQueryable().Returns(new List<SubscriptionPlan> { plan }.AsQueryable().BuildMock());
            _mapper.Map<SubscriptionPlanDto>(Arg.Any<SubscriptionPlan>()).Returns(new SubscriptionPlanDto { Name = plan.Name });
            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            // Act
            var result = await _sut.CreatePlanAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            await planRepo.Received(1).AddAsync(Arg.Any<SubscriptionPlan>());
        }

        [Fact]
        public async Task DeletePlanAsync_WithActiveSubscriptions_Returns409()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var activeSub = new UserSubscription
            {
                SubscriptionPlanId = planId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _subscriptionRepo.GetQueryable()
                .Returns(new List<UserSubscription> { activeSub }.AsQueryable().BuildMock());
            _messageService.Get("PlanHasActiveSubscriptions").Returns("Cannot delete a plan that has active subscriptions.");

            // Act
            var result = await _sut.DeletePlanAsync(planId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
        }

        [Fact]
        public async Task DeletePlanAsync_NoActiveSubscriptions_Succeeds()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var plan = new SubscriptionPlan { Id = planId };
            var planRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();
            _unitOfWork.Repository<SubscriptionPlan>().Returns(planRepo);

            _subscriptionRepo.GetQueryable()
                .Returns(new List<UserSubscription>().AsQueryable().BuildMock());
            planRepo.GetByIdAsync(planId).Returns(plan);
            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            // Act
            var result = await _sut.DeletePlanAsync(planId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(plan.IsDeleted);
        }

        [Fact]
        public async Task AssignTemplateAsync_Duplicate_Returns409()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var templateId = Guid.NewGuid();

            var planRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();
            var planTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();
            _unitOfWork.Repository<SubscriptionPlan>().Returns(planRepo);
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(planTemplateRepo);

            planRepo.GetQueryable().Returns(new List<SubscriptionPlan>
                { new() { Id = planId } }.AsQueryable().BuildMock());

            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate>
                { new() { Id = templateId, IsActive = true, IsDeleted = false } }.AsQueryable().BuildMock());

            planTemplateRepo.GetQueryable().Returns(new List<SubscriptionPlanTemplate>
                { new() { SubscriptionPlanId = planId, CardTemplateId = templateId } }.AsQueryable().BuildMock());

            _messageService.Get("TemplateAlreadyAssigned").Returns("Already assigned.");

            // Act
            var result = await _sut.AssignTemplateAsync(planId, templateId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
        }

        [Fact]
        public async Task AssignTemplateAsync_New_Succeeds()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var templateId = Guid.NewGuid();

            var planRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();
            var planTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();
            _unitOfWork.Repository<SubscriptionPlan>().Returns(planRepo);
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(planTemplateRepo);

            planRepo.GetQueryable().Returns(new List<SubscriptionPlan>
                { new() { Id = planId } }.AsQueryable().BuildMock());
            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate>
                { new() { Id = templateId, IsActive = true, IsDeleted = false } }.AsQueryable().BuildMock());
            planTemplateRepo.GetQueryable().Returns(new List<SubscriptionPlanTemplate>().AsQueryable().BuildMock());
            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            // Act
            var result = await _sut.AssignTemplateAsync(planId, templateId);

            // Assert
            Assert.True(result.IsSuccess);
            await planTemplateRepo.Received(1).AddAsync(Arg.Is<SubscriptionPlanTemplate>(
                pt => pt.SubscriptionPlanId == planId && pt.CardTemplateId == templateId));
        }

        [Fact]
        public async Task DeleteTemplateAsync_NullsOutUserAndCompanyProfiles()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            var template = new CardTemplate { Id = templateId, IsActive = true, IsDeleted = false };

            var profile1 = new UserProfile { ProfileTemplateId = templateId };
            var profile2 = new UserProfile { ProfileTemplateId = templateId };
            var company1 = new Company { ProfileTemplateId = templateId };

            var planTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(planTemplateRepo);

            _cardTemplateRepo.GetByIdAsync(templateId).Returns(template);
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile> { profile1, profile2 }.AsQueryable().BuildMock());
            _companyRepo.GetQueryable().Returns(new List<Company> { company1 }.AsQueryable().BuildMock());
            planTemplateRepo.GetQueryable().Returns(new List<SubscriptionPlanTemplate>().AsQueryable().BuildMock());
            _messageService.Get("TemplateDeletedAndProfilesCleared").Returns("Deleted.");

            // Act
            var result = await _sut.DeleteTemplateAsync(templateId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(template.IsDeleted);
            Assert.False(template.IsActive);
            Assert.Null(profile1.ProfileTemplateId);
            Assert.Null(profile2.ProfileTemplateId);
            Assert.Null(company1.ProfileTemplateId);
        }

        [Fact]
        public async Task UnassignTemplateAsync_NotFound_Returns404()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var templateId = Guid.NewGuid();

            var planTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(planTemplateRepo);
            planTemplateRepo.GetQueryable().Returns(new List<SubscriptionPlanTemplate>().AsQueryable().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Not found.");

            // Act
            var result = await _sut.UnassignTemplateAsync(planId, templateId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }
    }
}
