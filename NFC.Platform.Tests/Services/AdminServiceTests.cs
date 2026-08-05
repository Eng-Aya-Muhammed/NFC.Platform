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
        private readonly IGenericRepository<Employee> _employeeRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;
        private readonly IGenericRepository<Company> _companyRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;
        private readonly IGenericRepository<TemplateCategory> _templateCategoryRepo;
        private readonly IGenericRepository<SubscriptionPlanTemplate> _subscriptionPlanTemplateRepo;
        private readonly IGenericRepository<SubscriptionPlan> _subscriptionPlanRepo;
        private readonly IBackgroundJobClient _backgroundJobClient;

        private readonly AdminService _sut;

        public AdminServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();

            _storageService = Substitute.For<IStorageService>();
            _backgroundJobClient = Substitute.For<IBackgroundJobClient>();

            _orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            _templateRequestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();
            _cardTemplateRepo = Substitute.For<IGenericRepository<CardTemplate>>();
            _tenantRepo = Substitute.For<IGenericRepository<Tenant>>();
            _employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            _companyRepo = Substitute.For<IGenericRepository<Company>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _templateCategoryRepo = Substitute.For<IGenericRepository<TemplateCategory>>();
            _subscriptionPlanTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();
            _subscriptionPlanRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<TemplateRequest>().Returns(_templateRequestRepo);
            _unitOfWork.Repository<CardTemplate>().Returns(_cardTemplateRepo);
            _unitOfWork.Repository<Tenant>().Returns(_tenantRepo);
            _unitOfWork.Repository<Employee>().Returns(_employeeRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);
            _unitOfWork.Repository<Company>().Returns(_companyRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);
            _unitOfWork.Repository<TemplateCategory>().Returns(_templateCategoryRepo);
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(_subscriptionPlanTemplateRepo);
            _unitOfWork.Repository<SubscriptionPlan>().Returns(_subscriptionPlanRepo);

            _companyRepo.GetQueryable().Returns(new List<Company>().AsQueryable().BuildMock());
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());
            _templateCategoryRepo.GetQueryable().Returns(new List<TemplateCategory>().AsQueryable().BuildMock());
            _templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest>().AsQueryable().BuildMock());
            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate>().AsQueryable().BuildMock());
            _subscriptionPlanTemplateRepo.GetQueryable().Returns(new List<SubscriptionPlanTemplate>().AsQueryable().BuildMock());
            _subscriptionPlanRepo.GetQueryable().Returns(new List<SubscriptionPlan>().AsQueryable().BuildMock());



            _storageService
                .UploadBytesAsImageAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new UploadResultDto
                {
                    SecureUrl = "https://res.cloudinary.com/demo/image/upload/qr-placeholder.png",
                    PublicId = "nfc-platform/qrcodes/test/qr-placeholder"
                }));

            _sut = new AdminService(_unitOfWork, _mapper, _messageService, _storageService, _backgroundJobClient);
        }


        [Fact]
        public async Task GetOrdersPagedAsync_ReturnsAllOrders_WhenNoStatusFilterPassed()
        {
            var orders = new List<CardOrder>
            {
                new() { Id = Guid.NewGuid(), Status = OrderStatus.PendingReview },
                new() { Id = Guid.NewGuid(), Status = OrderStatus.UnderReview }
            };
            var mockQueryable = orders.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            _mapper.Map<AdminOrderSummaryDto>(Arg.Any<CardOrder>())
                .Returns(x => new AdminOrderSummaryDto { Id = ((CardOrder)x[0]).Id });

            var result = await _sut.GetOrdersPagedAsync(pagination, null);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Items.Count);
        }

        [Fact]
        public async Task GetOrdersPagedAsync_FiltersOrders_WhenStatusFilterPassed()
        {
            var orders = new List<CardOrder>
            {
                new() { Id = Guid.NewGuid(), Status = OrderStatus.PendingReview },
                new() { Id = Guid.NewGuid(), Status = OrderStatus.UnderReview }
            };
            var mockQueryable = orders.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            _mapper.Map<AdminOrderSummaryDto>(Arg.Any<CardOrder>())
                .Returns(x => new AdminOrderSummaryDto { Id = ((CardOrder)x[0]).Id, Status = ((CardOrder)x[0]).Status });

            var result = await _sut.GetOrdersPagedAsync(pagination, OrderStatus.UnderReview);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!.Items);
            Assert.Equal(OrderStatus.UnderReview, result.Data.Items.First().Status);
        }


        [Fact]
        public async Task GetOrderByIdAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            var mockQueryable = new List<CardOrder>().AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var result = await _sut.GetOrderByIdAsync(Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetOrderByIdAsync_ReturnsOrderDetail_WhenOrderExists()
        {
            var orderId = Guid.NewGuid();
            var orders = new List<CardOrder>
            {
                new() { Id = orderId }
            };
            var mockQueryable = orders.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var dto = new AdminOrderDetailDto { Id = orderId };
            _mapper.Map<AdminOrderDetailDto>(Arg.Any<CardOrder>()).Returns(dto);

            var result = await _sut.GetOrderByIdAsync(orderId);

            Assert.True(result.IsSuccess);
            Assert.Equal(orderId, result.Data!.Id);
        }


        [Fact]
        public async Task UpdateOrderStatusAsync_UpdatesStatusAndTracking_WhenOrderExists()
        {
            var orderId = Guid.NewGuid();
            var order = new CardOrder { Id = orderId, Status = OrderStatus.Approved };
            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var updateDto = new UpdateOrderStatusDto
            {
                Status = OrderStatus.ReadyForDelivery,
                TrackingNumber = "TRK12345"
            };

            var result = await _sut.UpdateOrderStatusAsync(orderId, updateDto);

            Assert.True(result.IsSuccess);
            Assert.Equal(OrderStatus.ReadyForDelivery, order.Status);
            Assert.Equal("TRK12345", order.TrackingNumber);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ReturnsError_WhenStatusTransitionIsInvalid()
        {
            var orderId = Guid.NewGuid();
            var order = new CardOrder { Id = orderId, Status = OrderStatus.Delivered };
            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);

            var updateDto = new UpdateOrderStatusDto
            {
                Status = OrderStatus.PendingReview
            };

            var result = await _sut.UpdateOrderStatusAsync(orderId, updateDto);

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            _messageService.Received(1).Get("InvalidStatusTransition", Arg.Any<string>(), Arg.Any<string>());
        }


        [Fact]
        public async Task ResolveTemplateRequestAsync_CreatesCustomTemplate_WhenApproved()
        {
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

            var result = await _sut.ResolveTemplateRequestAsync(requestId, resolveDto);

            Assert.True(result.IsSuccess);
            Assert.Equal(TemplateRequestStatus.Completed, request.Status);
            Assert.Contains("Admin Notes: Design complete", request.Notes);

            await _cardTemplateRepo.Received(1).AddAsync(Arg.Is<CardTemplate>(t =>
                t.NameAr == "Corporate Template 1" &&
                t.NameEn == "Corporate Template 1"));

            await _unitOfWork.Received(1).SaveChangesAsync();
        }


        [Fact]
        public async Task CreateTemplateAsync_SavesTemplateAndReturnsDto()
        {
            var createDto = new CreateCardTemplateRequest
            {
                NameAr = "Modern Template",
                NameEn = "Modern Template",
                CategoryId = Guid.NewGuid()
            };

            var mappedTemplate = new CardTemplate
            {
                NameAr = "Modern Template",
                NameEn = "Modern Template",
                CategoryId = createDto.CategoryId
            };

            _mapper.Map<CardTemplate>(createDto).Returns(mappedTemplate);
            _mapper.Map<CardTemplateAdminDto>(mappedTemplate).Returns(new CardTemplateAdminDto { NameAr = "Modern Template", NameEn = "Modern Template" });

            var result = await _sut.CreateTemplateAsync(createDto);

            Assert.True(result.IsSuccess);
            Assert.Equal("Modern Template", result.Data!.NameAr);
            await _cardTemplateRepo.Received(1).AddAsync(mappedTemplate);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }


        [Fact]
        public async Task UpdateTemplateAsync_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            var templateId = Guid.NewGuid();
            _cardTemplateRepo.GetByIdAsync(templateId).Returns((CardTemplate?)null);

            var result = await _sut.UpdateTemplateAsync(templateId, new UpdateCardTemplateRequest());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateTemplateAsync_UpdatesValues_WhenTemplateExists()
        {
            var templateId = Guid.NewGuid();
            var template = new CardTemplate { Id = templateId, NameAr = "Old Name", NameEn = "Old Name" };
            _cardTemplateRepo.GetByIdAsync(templateId).Returns(template);

            var updateDto = new UpdateCardTemplateRequest { NameAr = "New Name", NameEn = "New Name" };
            _mapper.Map(updateDto, template).Returns(template);
            _mapper.Map<CardTemplateAdminDto>(template).Returns(new CardTemplateAdminDto { Id = templateId, NameAr = "New Name", NameEn = "New Name" });

            var result = await _sut.UpdateTemplateAsync(templateId, updateDto);

            Assert.True(result.IsSuccess);
            Assert.Equal("New Name", result.Data!.NameAr);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }



        [Fact]
        public async Task GetTenantsPagedAsync_ReturnsPagedTenantsWithSubscriptions()
        {
            var tenantId = Guid.NewGuid();
            var tenants = new List<Tenant>
            {
                new() { Id = tenantId, Name = "ACME Corp", IsActive = true }
            };
            var mockQueryable = tenants.AsQueryable().BuildMock();
            _tenantRepo.GetQueryable().Returns(mockQueryable);

            var subscriptions = new List<UserSubscription>
            {
                new() { TenantId = tenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(30), SubscriptionPlan = new SubscriptionPlan { NameAr = "Premium Plan Ar", NameEn = "Premium Plan En" } }
            };
            var mockSubQuery = subscriptions.AsQueryable().BuildMock();
            _subscriptionRepo.GetQueryable().Returns(mockSubQuery);

            _mapper.Map<TenantSummaryDto>(Arg.Any<Tenant>()).Returns(new TenantSummaryDto { Id = tenantId, Name = "ACME Corp", IsActive = true });

            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetTenantsPagedAsync(pagination);

            Assert.True(result.IsSuccess);
            var item = result.Data!.Items.First();
            Assert.Equal("Premium Plan En", item.ActivePlanName);
            Assert.True(item.DaysRemaining > 0);
        }


        [Fact]
        public async Task UpdateTenantStatusAsync_TogglesTenantActiveState()
        {
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant { Id = tenantId, IsActive = true };
            _tenantRepo.GetByIdAsync(tenantId).Returns(tenant);

            var updateDto = new UpdateTenantStatusDto { IsActive = false };

            var result = await _sut.UpdateTenantStatusAsync(tenantId, updateDto);

            Assert.True(result.IsSuccess);
            Assert.False(tenant.IsActive);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateTemplateAsync_CreatesGlobalTemplate()
        {
            var dto = new CreateCardTemplateRequest { NameAr = "Global Temp", NameEn = "Global Temp" };
            var mappedTemplate = new CardTemplate { NameAr = "Global Temp", NameEn = "Global Temp" };
            _mapper.Map<CardTemplate>(dto).Returns(mappedTemplate);

            var expectedDto = new CardTemplateAdminDto { NameAr = "Global Temp", NameEn = "Global Temp" };
            _mapper.Map<CardTemplateAdminDto>(mappedTemplate).Returns(expectedDto);

            var result = await _sut.CreateTemplateAsync(dto);

            Assert.True(result.IsSuccess);
            await _cardTemplateRepo.Received(1).AddAsync(mappedTemplate);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteTemplateAsync_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            var id = Guid.NewGuid();
            _cardTemplateRepo.GetByIdAsync(id).Returns((CardTemplate?)null);

            var result = await _sut.DeleteTemplateAsync(id);

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            var id = Guid.NewGuid();
            _templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest>().AsQueryable().BuildMock());

            var result = await _sut.ResolveTemplateRequestAsync(id, new ResolveTemplateRequestDto());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_EnqueuesEmailNotification_WhenStatusCompleted()
        {
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

            var result = await _sut.ResolveTemplateRequestAsync(requestId, dto);

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

            var result = await _sut.ResolveTemplateRequestAsync(requestId, dto);

            Assert.True(result.IsSuccess);
            Assert.Equal(TemplateRequestStatus.Rejected, templateRequest.Status);
            Assert.Null(templateRequest.ProducedTemplateId);

            _backgroundJobClient.DidNotReceive().Create(
                Arg.Is<Hangfire.Common.Job>(j =>
                    j.Method.Name == nameof(IEmailService.SendTemplateRequestApprovedEmailAsync)),
                Arg.Any<Hangfire.States.IState>());
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_AppliesTemplateToCompanyProfile_WhenCompanyExists()
        {
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

            var result = await _sut.ResolveTemplateRequestAsync(requestId, dto);

            Assert.True(result.IsSuccess);
            Assert.NotNull(templateRequest.ProducedTemplateId);
            Assert.Equal(templateRequest.ProducedTemplateId, company.ProfileTemplateId);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task ResolveTemplateRequestAsync_AppliesTemplateToUserProfile_WhenCompanyDoesNotExist()
        {
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
            _companyRepo.GetQueryable().Returns(new List<Company>().AsQueryable().BuildMock());
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile> { userProfile }.AsQueryable().BuildMock());
            _messageService.Get("RecordUpdated").Returns("Record updated successfully.");

            var dto = new ResolveTemplateRequestDto
            {
                Status = TemplateRequestStatus.Completed,
                StyleConfigJson = "{\"theme\":\"minimalist\"}"
            };

            var result = await _sut.ResolveTemplateRequestAsync(requestId, dto);

            Assert.True(result.IsSuccess);
            Assert.NotNull(templateRequest.ProducedTemplateId);
            Assert.Equal(templateRequest.ProducedTemplateId, userProfile.ProfileTemplateId);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }





        [Fact]
        public async Task VerifyDeliveryOtpAsync_ReturnsFail_WhenOtpHasExpired()
        {
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = orderId,
                Status = OrderStatus.ReadyForDelivery,
                DeliveryOtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("123456"),
                DeliveryOtpExpiresAt = DateTime.UtcNow.AddMinutes(-5)
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpExpired").Returns("OTP code has expired.");

            var result = await _sut.VerifyDeliveryOtpAsync(orderId, "123456");

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal("OTP code has expired.", result.Message);
            Assert.Equal(OrderStatus.ReadyForDelivery, order.Status);
        }

        [Fact]
        public async Task VerifyDeliveryOtpAsync_IncrementsFailedAttempts_WhenOtpIsIncorrect()
        {
            var orderId = Guid.NewGuid();
            var expectedHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("123456");
            var order = new CardOrder
            {
                Id = orderId,
                Status = OrderStatus.ReadyForDelivery,
                DeliveryOtpHash = expectedHash,
                DeliveryOtpExpiresAt = DateTime.UtcNow.AddMinutes(5),
                DeliveryOtpFailedAttempts = 1
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("InvalidOtp").Returns("Invalid OTP.");

            var result = await _sut.VerifyDeliveryOtpAsync(orderId, "000000");

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal("Invalid OTP.", result.Message);
            Assert.Equal(2, order.DeliveryOtpFailedAttempts);
            Assert.Equal(expectedHash, order.DeliveryOtpHash);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task VerifyDeliveryOtpAsync_InvalidatesOtp_WhenMaxFailedAttemptsReached()
        {
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = orderId,
                Status = OrderStatus.ReadyForDelivery,
                DeliveryOtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("123456"),
                DeliveryOtpExpiresAt = DateTime.UtcNow.AddMinutes(5),
                DeliveryOtpFailedAttempts = 4
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpExpired").Returns("OTP code has expired.");

            var result = await _sut.VerifyDeliveryOtpAsync(orderId, "999999");

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal(5, order.DeliveryOtpFailedAttempts);
            Assert.Null(order.DeliveryOtpHash);
            Assert.Null(order.DeliveryOtpExpiresAt);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task VerifyDeliveryOtpAsync_ResetsFailedAttempts_WhenOtpIsCorrect()
        {
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = orderId,
                Status = OrderStatus.ReadyForDelivery,
                DeliveryOtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("123456"),
                DeliveryOtpExpiresAt = DateTime.UtcNow.AddMinutes(5),
                DeliveryOtpFailedAttempts = 3
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OrderDelivered").Returns("Order delivered.");

            var result = await _sut.VerifyDeliveryOtpAsync(orderId, "123456");

            Assert.True(result.IsSuccess);
            Assert.Equal(OrderStatus.Delivered, order.Status);
            Assert.Null(order.DeliveryOtpHash);
            Assert.Equal(0, order.DeliveryOtpFailedAttempts);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsFail_WhenCooldownActive()
        {
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = orderId,
                Status = OrderStatus.ReadyForDelivery,
                DeliveryOtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("123456"),
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddSeconds(-30),
                DeliveryOtpResendCount = 1
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpCooldownActive").Returns("Please wait 60 seconds.");

            var result = await _sut.ResendDeliveryOtpAsync(orderId);

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal("Please wait 60 seconds.", result.Message);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsFail_WhenResendLimitReached()
        {
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = orderId,
                Status = OrderStatus.ReadyForDelivery,
                DeliveryOtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("123456"),
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddMinutes(-10),
                DeliveryOtpResendCount = 5
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpResendLimitReached").Returns("Limit reached.");

            var result = await _sut.ResendDeliveryOtpAsync(orderId);

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal("Limit reached.", result.Message);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_GeneratesNewOtp_ResetsExpiry_IncrementsCount_AndEnqueuesJobs()
        {
            var orderId = Guid.NewGuid();
            var user = new User
            {
                Email = "customer@example.com",
                UserProfile = new UserProfile { WhatsApp = "+201013503890" }
            };
            var order = new CardOrder
            {
                Id = orderId,
                Status = OrderStatus.ReadyForDelivery,
                DeliveryOtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("111111"),
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddMinutes(-2),
                DeliveryOtpResendCount = 2,
                Tenant = new Tenant { Company = null },
                User = user
            };

            var mockQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockQueryable);
            _messageService.Get("OtpResent").Returns("OTP code has been resent successfully.");

            var result = await _sut.ResendDeliveryOtpAsync(orderId);

            Assert.True(result.IsSuccess);
            Assert.Equal("OTP code has been resent successfully.", result.Message);
            Assert.NotEqual(NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("111111"), order.DeliveryOtpHash);
            Assert.NotNull(order.DeliveryOtpHash);
            Assert.Equal(3, order.DeliveryOtpResendCount);
            Assert.NotNull(order.DeliveryOtpExpiresAt);
            Assert.True(order.DeliveryOtpExpiresAt > DateTime.UtcNow);

            await _unitOfWork.Received(1).SaveChangesAsync();

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


        [Fact]
        public async Task CreatePlanAsync_ValidRequest_ReturnsSuccess()
        {
            var request = new CreateSubscriptionPlanRequest
            {
                NameAr = "BusinessAr",
                NameEn = "BusinessEn",
                Features = ["Business plan"],
                Price = 199m,
                DurationInDays = 365,
                MaxTemplateChanges = 5,
                MaxCustomDesignRequests = 2
            };

            var planRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();
            var planTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();

            _unitOfWork.Repository<SubscriptionPlan>().Returns(planRepo);
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(planTemplateRepo);

            var plan = new SubscriptionPlan { Id = Guid.NewGuid(), NameAr = request.NameAr, NameEn = request.NameEn };
            _mapper.Map<SubscriptionPlan>(request).Returns(plan);

            planRepo.GetQueryable().Returns(new List<SubscriptionPlan>().AsQueryable().BuildMock());
            _mapper.Map<SubscriptionPlanAdminDto>(Arg.Any<SubscriptionPlan>()).Returns(new SubscriptionPlanAdminDto { NameAr = plan.NameAr, NameEn = plan.NameEn });
            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            var result = await _sut.CreatePlanAsync(request);

            Assert.True(result.IsSuccess);
            await planRepo.Received(1).AddAsync(Arg.Any<SubscriptionPlan>());
        }

        [Fact]
        public async Task DeletePlanAsync_WithActiveSubscriptions_Returns409()
        {
            var planId = Guid.NewGuid();
            var plan = new SubscriptionPlan { Id = planId };
            _subscriptionPlanRepo.GetByIdAsync(planId).Returns(plan);

            var activeSub = new UserSubscription
            {
                SubscriptionPlanId = planId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _subscriptionRepo.GetQueryable()
                .Returns(new List<UserSubscription> { activeSub }.AsQueryable().BuildMock());
            _messageService.Get("PlanHasActiveSubscriptions").Returns("Cannot delete a plan that has active subscriptions.");

            var result = await _sut.DeletePlanAsync(planId);

            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
        }

        [Fact]
        public async Task DeletePlanAsync_NoActiveSubscriptions_Succeeds()
        {
            var planId = Guid.NewGuid();
            var plan = new SubscriptionPlan { Id = planId };

            _subscriptionRepo.GetQueryable()
                .Returns(new List<UserSubscription>().AsQueryable().BuildMock());
            _subscriptionPlanRepo.GetByIdAsync(planId).Returns(plan);
            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            var result = await _sut.DeletePlanAsync(planId);

            Assert.True(result.IsSuccess);
            _subscriptionPlanRepo.Received(1).Remove(plan);
        }

        [Fact]
        public async Task AssignTemplateAsync_Duplicate_Returns409()
        {
            var planId = Guid.NewGuid();
            var templateId = Guid.NewGuid();

            _subscriptionPlanRepo.GetByIdAsync(planId).Returns(new SubscriptionPlan { Id = planId });
            _cardTemplateRepo.GetByIdAsync(templateId).Returns(new CardTemplate { Id = templateId });

            _subscriptionPlanTemplateRepo.GetQueryable().Returns(new List<SubscriptionPlanTemplate>
                { new() { SubscriptionPlanId = planId, CardTemplateId = templateId } }.AsQueryable().BuildMock());

            _messageService.Get("TemplateAlreadyAssigned").Returns("Already assigned.");

            var result = await _sut.AssignTemplateAsync(planId, templateId);

            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
        }

        [Fact]
        public async Task AssignTemplateAsync_New_Succeeds()
        {
            var planId = Guid.NewGuid();
            var templateId = Guid.NewGuid();

            _subscriptionPlanRepo.GetByIdAsync(planId).Returns(new SubscriptionPlan { Id = planId });
            _cardTemplateRepo.GetByIdAsync(templateId).Returns(new CardTemplate { Id = templateId });

            _subscriptionPlanTemplateRepo.GetQueryable().Returns(new List<SubscriptionPlanTemplate>().AsQueryable().BuildMock());
            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            var result = await _sut.AssignTemplateAsync(planId, templateId);

            Assert.True(result.IsSuccess);
            await _subscriptionPlanTemplateRepo.Received(1).AddAsync(Arg.Is<SubscriptionPlanTemplate>(
                pt => pt.SubscriptionPlanId == planId && pt.CardTemplateId == templateId));
        }

        [Fact]
        public async Task DeleteTemplateAsync_NullsOutUserAndCompanyProfiles()
        {
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

            var result = await _sut.DeleteTemplateAsync(templateId);

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
            var planId = Guid.NewGuid();
            var templateId = Guid.NewGuid();

            var planTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(planTemplateRepo);
            planTemplateRepo.GetQueryable().Returns(new List<SubscriptionPlanTemplate>().AsQueryable().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Not found.");

            var result = await _sut.UnassignTemplateAsync(planId, templateId);

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetTenantBasicInfoAsync_ReturnsSuccess_WhenTenantExists()
        {
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant { Id = tenantId, Name = "Test Tenant" };
            var query = new List<Tenant> { tenant }.AsQueryable().BuildMock();

            _tenantRepo.GetQueryable().Returns(query);

            var dto = new TenantBasicInfoDto { Id = tenantId, CompanyName = "Test Tenant" };
            _mapper.Map<TenantBasicInfoDto>(Arg.Any<Tenant>()).Returns(dto);

            var result = await _sut.GetTenantBasicInfoAsync(tenantId);

            Assert.True(result.IsSuccess);
            Assert.Equal("Test Tenant", result.Data!.CompanyName);
        }

        [Fact]
        public async Task GetTenantEmployeesPagedAsync_ReturnsPagedEmployees()
        {
            var tenantId = Guid.NewGuid();
            var employee = new Employee { Id = Guid.NewGuid(), TenantId = tenantId, FullName = "John", IsDeleted = false };
            var query = new List<Employee> { employee }.AsQueryable().BuildMock();

            _employeeRepo.GetQueryable().Returns(query);

            var dto = new EmployeeDto { Id = employee.Id, FullName = "John" };
            _mapper.Map<EmployeeDto>(Arg.Any<Employee>()).Returns(dto);

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetTenantEmployeesPagedAsync(tenantId, request);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!.Items);
            Assert.Equal("John", result.Data!.Items.First().FullName);
        }

        [Fact]
        public async Task GetTenantEmployeeDetailsAsync_ReturnsSuccess_WhenEmployeeExists()
        {
            var tenantId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var employee = new Employee { Id = employeeId, TenantId = tenantId, FullName = "John", IsDeleted = false };
            var query = new List<Employee> { employee }.AsQueryable().BuildMock();

            _employeeRepo.GetQueryable().Returns(query);

            var dto = new EmployeeDetailsDto { Id = employee.Id, FullName = "John" };
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<Employee>()).Returns(dto);

            var result = await _sut.GetTenantEmployeeDetailsAsync(tenantId, employeeId);

            Assert.True(result.IsSuccess);
            Assert.Equal("John", result.Data!.FullName);
        }
        [Fact]
        public async Task GetOrderByIdAsync_ReturnsSuccess_WithCustomerProfile_WhenOrderExists()
        {
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FullName = "John Admin",
                ContactEmail = "john@admin.com"
            };

            var user = new User { Id = userId, UserProfile = userProfile };

            var order = new CardOrder
            {
                Id = orderId,
                UserId = userId,
                User = user,
                Tenant = new Tenant { Name = "Tenant" }
            };

            var query = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(query);

            var dto = new AdminOrderDetailDto
            {
                Id = orderId,
                CustomerProfile = new AdminOrderCustomerProfileDto
                {
                    FullName = "John Admin",
                    ContactEmail = "john@admin.com"
                }
            };

            _mapper.Map<AdminOrderDetailDto>(Arg.Any<CardOrder>()).Returns(dto);

            var result = await _sut.GetOrderByIdAsync(orderId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data!.CustomerProfile);
            Assert.Equal("John Admin", result.Data.CustomerProfile!.FullName);
            Assert.Equal("john@admin.com", result.Data.CustomerProfile.ContactEmail);
        }

        [Fact]
        public async Task GetOrdersPagedAsync_FiltersBySearch_MatchesTrackingNumber()
        {
            var order1 = new CardOrder { Id = Guid.NewGuid(), TrackingNumber = "TRK_ALPHA" };
            var order2 = new CardOrder { Id = Guid.NewGuid(), TrackingNumber = "TRK_BETA" };

            var query = new List<CardOrder> { order1, order2 }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(query);
            _mapper.Map<AdminOrderSummaryDto>(Arg.Any<CardOrder>()).Returns(x => new AdminOrderSummaryDto { Id = ((CardOrder)x[0]).Id });

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetOrdersPagedAsync(request, null, null, null, "ALPHA");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal(order1.Id, result.Data.Items.First().Id);
        }

        [Fact]
        public async Task GetTenantsPagedAsync_FiltersBySearch_MatchesCompanyName()
        {
            var t1 = new Tenant { Id = Guid.NewGuid(), Name = "T1", Company = new Company { Name = "Alpha Tech" } };
            var t2 = new Tenant { Id = Guid.NewGuid(), Name = "T2", Company = new Company { Name = "Beta Solutions" } };

            var query = new List<Tenant> { t1, t2 }.AsQueryable().BuildMock();
            _tenantRepo.GetQueryable().Returns(query);
            _mapper.Map<TenantSummaryDto>(Arg.Any<Tenant>()).Returns(x => new TenantSummaryDto { Id = ((Tenant)x[0]).Id, Name = ((Tenant)x[0]).Company?.Name ?? ((Tenant)x[0]).Name });

            _unitOfWork.Repository<UserSubscription>().GetQueryable().Returns(new List<UserSubscription>().AsQueryable().BuildMock());

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetTenantsPagedAsync(request, "Alpha");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal("Alpha Tech", result.Data.Items.First().Name);
        }

        [Fact]
        public async Task GetTenantEmployeesPagedAsync_FiltersBySearch_MatchesJobTitle()
        {
            var tenantId = Guid.NewGuid();
            var emp1 = new Employee { Id = Guid.NewGuid(), TenantId = tenantId, FullName = "Dev One", JobTitle = "Senior Engineer", IsDeleted = false };
            var emp2 = new Employee { Id = Guid.NewGuid(), TenantId = tenantId, FullName = "Sales One", JobTitle = "Account Manager", IsDeleted = false };

            var query = new List<Employee> { emp1, emp2 }.AsQueryable().BuildMock();
            _employeeRepo.GetQueryable().Returns(query);
            _mapper.Map<EmployeeDto>(Arg.Any<Employee>()).Returns(x => new EmployeeDto { Id = ((Employee)x[0]).Id, FullName = ((Employee)x[0]).FullName });

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetTenantEmployeesPagedAsync(tenantId, request, "Engineer");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal("Dev One", result.Data.Items.First().FullName);
        }

        [Fact]
        public async Task GetTenantsPagedAsync_HandlesNullCompany_WithoutCrashing()
        {
            var tenantWithoutCompany = new Tenant { Id = Guid.NewGuid(), Name = "Individual Tenant", Company = null };

            var query = new List<Tenant> { tenantWithoutCompany }.AsQueryable().BuildMock();
            _tenantRepo.GetQueryable().Returns(query);
            _mapper.Map<TenantSummaryDto>(Arg.Any<Tenant>()).Returns(new TenantSummaryDto());

            _unitOfWork.Repository<UserSubscription>().GetQueryable().Returns(new List<UserSubscription>().AsQueryable().BuildMock());

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetTenantsPagedAsync(request, "NonExistentSearchTerm");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Data!.TotalCount);
        }

        [Fact]
        public async Task GetOrdersPagedAsync_WhenSearchNullOrEmpty_ReturnsAllOrders()
        {
            var o1 = new CardOrder { Id = Guid.NewGuid() };
            var o2 = new CardOrder { Id = Guid.NewGuid() };

            var query = new List<CardOrder> { o1, o2 }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(query);
            _mapper.Map<AdminOrderSummaryDto>(Arg.Any<CardOrder>()).Returns(new AdminOrderSummaryDto());

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var resultWithNull = await _sut.GetOrdersPagedAsync(request, null, null, null, null);
            var resultWithSpaces = await _sut.GetOrdersPagedAsync(request, null, null, null, "   ");

            Assert.True(resultWithNull.IsSuccess);
            Assert.Equal(2, resultWithNull.Data!.TotalCount);
            Assert.True(resultWithSpaces.IsSuccess);
            Assert.Equal(2, resultWithSpaces.Data!.TotalCount);
        }

        [Fact]
        public async Task GetSubdomainsPagedAsync_FiltersBySearch()
        {
            var profile1 = new UserProfile { Id = Guid.NewGuid(), Subdomain = "alpha-slug", FullName = "Alpha User", IsDeleted = false };
            var profile2 = new UserProfile { Id = Guid.NewGuid(), Subdomain = "beta-slug", FullName = "Beta User", IsDeleted = false };

            var query = new List<UserProfile> { profile1, profile2 }.AsQueryable().BuildMock();
            _unitOfWork.Repository<UserProfile>().GetQueryable().Returns(query);
            _mapper.Map<ProfileSubdomainSummaryDto>(Arg.Any<UserProfile>()).Returns(x => new ProfileSubdomainSummaryDto { Subdomain = ((UserProfile)x[0]).Subdomain });

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetSubdomainsPagedAsync(request, "alpha");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal("alpha-slug", result.Data.Items.First().Subdomain);
        }

        [Fact]
        public async Task GetAllAdminPlansAsync_FiltersBySearch()
        {
            var plan1 = new SubscriptionPlan { Id = Guid.NewGuid(), NameAr = "خطة البداية", NameEn = "Starter Plan" };
            var plan2 = new SubscriptionPlan { Id = Guid.NewGuid(), NameAr = "خطة الشركات", NameEn = "Enterprise Plan" };

            var query = new List<SubscriptionPlan> { plan1, plan2 }.AsQueryable().BuildMock();
            _unitOfWork.Repository<SubscriptionPlan>().GetQueryable().Returns(query);
            _mapper.Map<SubscriptionPlanAdminDto>(Arg.Any<SubscriptionPlan>()).Returns(x => new SubscriptionPlanAdminDto { NameEn = ((SubscriptionPlan)x[0]).NameEn });

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            var result = await _sut.GetAllAdminPlansAsync(request, "Enterprise");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal("Enterprise Plan", result.Data.Items.First().NameEn);
        }
    }
}
