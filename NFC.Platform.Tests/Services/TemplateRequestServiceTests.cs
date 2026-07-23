namespace NFC.Platform.Tests.Services
{
    public class TemplateRequestServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        private readonly IGenericRepository<TemplateRequest> _templateRequestRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;

        private readonly TemplateRequestService _sut;

        public TemplateRequestServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _templateRequestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            
            _unitOfWork.Repository<TemplateRequest>().Returns(_templateRequestRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);

            _sut = new TemplateRequestService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        //  CreateRequestAsync 

        [Fact]
        public async Task CreateRequestAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);
            _messageService.Get("Unauthorized").Returns("Unauthorized.");

            var request = new CreateTemplateRequest { TemplateName = "Premium Blue" };

            // Act
            var result = await _sut.CreateRequestAsync(Guid.NewGuid(), request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsUnauthorized_WithFallbackMessage_WhenMessageIsEmpty()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);
            _messageService.Get("Unauthorized").Returns(string.Empty);

            // Act
            var result = await _sut.CreateRequestAsync(Guid.NewGuid(), new CreateTemplateRequest());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            Assert.NotEmpty(result.Message!);
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsFail_WhenLimitReached()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            var request = new CreateTemplateRequest { TemplateName = "Premium Blue" };

            var activeSub = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30),
                CustomDesignRequestsUsed = 2,
                SubscriptionPlan = new SubscriptionPlan { MaxCustomDesignRequests = 2 }
            };

            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { activeSub }.AsQueryable().BuildMock());
            _messageService.Get("CustomDesignRequestLimitReached").Returns("Limit reached");

            // Act
            var result = await _sut.CreateRequestAsync(userId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsSuccess_AndIncrementsCounter()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var request = new CreateTemplateRequest
            {
                TemplateName = "Premium Blue",
                LogoUrl = "https://logo.png",
                ReferenceImageUrl = "https://ref.png",
                Notes = "Make it pop"
            };

            var activeSub = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30),
                CustomDesignRequestsUsed = 1,
                SubscriptionPlan = new SubscriptionPlan { MaxCustomDesignRequests = 5 }
            };

            var dto = new TemplateRequestDto { TemplateName = "Premium Blue", Status = "Pending" };

            var createdQueryable = new List<TemplateRequest>
            {
                new() { Id = Guid.NewGuid(), Status = TemplateRequestStatus.Pending, RequestedByUser = new User() }
            }.AsQueryable().BuildMock();

            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { activeSub }.AsQueryable().BuildMock());
            _templateRequestRepo.GetQueryable().Returns(createdQueryable);
            _mapper.Map<TemplateRequest>(request).Returns(new TemplateRequest
            {
                TemplateName = request.TemplateName,
                LogoUrl = request.LogoUrl,
                ReferenceImageUrl = request.ReferenceImageUrl,
                Notes = request.Notes
            });
            _mapper.Map<TemplateRequestDto>(Arg.Any<TemplateRequest>()).Returns(dto);
            _messageService.Get("RecordCreated").Returns("Record created.");

            // Act
            var result = await _sut.CreateRequestAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Pending", result.Data!.Status);
            Assert.Equal(2, activeSub.CustomDesignRequestsUsed); // Incremented

            await _templateRequestRepo.Received(1).AddAsync(Arg.Is<TemplateRequest>(r =>
                r.RequestedByUserId == userId &&
                r.Status == TemplateRequestStatus.Pending &&
                r.TemplateName == "Premium Blue"));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        //  GetTenantRequestsAsync 

        [Fact]
        public async Task GetTenantRequestsAsync_ReturnsEmptyList_WhenNoRequestsExist()
        {
            // Arrange
            var emptyQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(emptyQueryable);
            _mapper.Map<IReadOnlyList<TemplateRequestDto>>(Arg.Any<object>())
                .Returns(new List<TemplateRequestDto>());

            // Act
            var result = await _sut.GetTenantRequestsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetTenantRequestsAsync_ReturnsRequestsOrderedByDateDescending()
        {
            // Arrange
            var older = new TemplateRequest { Id = Guid.NewGuid(), TemplateName = "First", CreatedAt = DateTime.UtcNow.AddDays(-2), RequestedByUser = new User() };
            var newer = new TemplateRequest { Id = Guid.NewGuid(), TemplateName = "Second", CreatedAt = DateTime.UtcNow,            RequestedByUser = new User() };

            var queryable = new List<TemplateRequest> { older, newer }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dtos = new List<TemplateRequestDto>
            {
                new() { TemplateName = "Second" },
                new() { TemplateName = "First" }
            };
            _mapper.Map<IReadOnlyList<TemplateRequestDto>>(Arg.Any<object>()).Returns(dtos);

            // Act
            var result = await _sut.GetTenantRequestsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal("Second", result.Data![0].TemplateName);
        }

        //  GetRequestByIdAsync 

        [Fact]
        public async Task GetRequestByIdAsync_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var emptyQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.GetRequestByIdAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("Record not found.", result.Message);
        }

        [Fact]
        public async Task GetRequestByIdAsync_ReturnsSuccess_WhenRequestExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var templateRequest = new TemplateRequest { Id = id, TemplateName = "Premium" };
            var queryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dto = new TemplateRequestDto { Id = id, TemplateName = "Premium" };
            _mapper.Map<TemplateRequestDto>(templateRequest).Returns(dto);

            // Act
            var result = await _sut.GetRequestByIdAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Premium", result.Data!.TemplateName);
        }
    }
}
