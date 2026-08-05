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


        [Fact]
        public async Task CreateRequestAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            _currentTenant.TenantId.Returns((Guid?)null);
            _messageService.Get("Unauthorized").Returns("Unauthorized.");

            var request = new CreateTemplateRequest { TemplateName = "Premium Blue" };

            var result = await _sut.CreateRequestAsync(Guid.NewGuid(), request);

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsUnauthorized_WithFallbackMessage_WhenMessageIsEmpty()
        {
            _currentTenant.TenantId.Returns((Guid?)null);
            _messageService.Get("Unauthorized").Returns(string.Empty);

            var result = await _sut.CreateRequestAsync(Guid.NewGuid(), new CreateTemplateRequest());

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            Assert.NotEmpty(result.Message!);
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsFail_WhenLimitReached()
        {
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

            var result = await _sut.CreateRequestAsync(userId, request);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsSuccess_AndIncrementsCounter()
        {
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

            var result = await _sut.CreateRequestAsync(userId, request);

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Pending", result.Data!.Status);
            Assert.Equal(2, activeSub.CustomDesignRequestsUsed);

            await _templateRequestRepo.Received(1).AddAsync(Arg.Is<TemplateRequest>(r =>
                r.RequestedByUserId == userId &&
                r.Status == TemplateRequestStatus.Pending &&
                r.TemplateName == "Premium Blue"));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }


        [Fact]
        public async Task GetTenantRequestsAsync_ReturnsEmptyList_WhenNoRequestsExist()
        {
            var emptyQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(emptyQueryable);
            _mapper.Map<IReadOnlyList<TemplateRequestDto>>(Arg.Any<object>())
                .Returns(new List<TemplateRequestDto>());

            var result = await _sut.GetTenantRequestsAsync();

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetTenantRequestsAsync_ReturnsRequestsOrderedByDateDescending()
        {
            var older = new TemplateRequest { Id = Guid.NewGuid(), TemplateName = "First", CreatedAt = DateTime.UtcNow.AddDays(-2), RequestedByUser = new User() };
            var newer = new TemplateRequest { Id = Guid.NewGuid(), TemplateName = "Second", CreatedAt = DateTime.UtcNow, RequestedByUser = new User() };

            var queryable = new List<TemplateRequest> { older, newer }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dtos = new List<TemplateRequestDto>
            {
                new() { TemplateName = "Second" },
                new() { TemplateName = "First" }
            };
            _mapper.Map<IReadOnlyList<TemplateRequestDto>>(Arg.Any<object>()).Returns(dtos);

            var result = await _sut.GetTenantRequestsAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal("Second", result.Data![0].TemplateName);
        }


        [Fact]
        public async Task GetRequestByIdAsync_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            var id = Guid.NewGuid();
            var emptyQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var result = await _sut.GetRequestByIdAsync(id);

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("Record not found.", result.Message);
        }

        [Fact]
        public async Task GetRequestByIdAsync_ReturnsSuccess_WhenRequestExists()
        {
            var id = Guid.NewGuid();
            var templateRequest = new TemplateRequest { Id = id, TemplateName = "Premium" };
            var queryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dto = new TemplateRequestDto { Id = id, TemplateName = "Premium" };
            _mapper.Map<TemplateRequestDto>(templateRequest).Returns(dto);

            var result = await _sut.GetRequestByIdAsync(id);

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Premium", result.Data!.TemplateName);
        }

        [Fact]
        public async Task UpdateRequestAsync_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            var id = Guid.NewGuid();
            var emptyQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var result = await _sut.UpdateRequestAsync(id, Guid.NewGuid(), new UpdateTemplateRequest());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateRequestAsync_ReturnsFail_WhenStatusIsNotPending()
        {
            var id = Guid.NewGuid();
            var templateRequest = new TemplateRequest { Id = id, Status = TemplateRequestStatus.InProgress };
            var queryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();

            _templateRequestRepo.GetQueryable().Returns(queryable);
            _messageService.Get("TemplateRequestCannotBeUpdated").Returns("Cannot update.");

            var result = await _sut.UpdateRequestAsync(id, Guid.NewGuid(), new UpdateTemplateRequest());

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Cannot update.", result.Message);
        }

        [Fact]
        public async Task UpdateRequestAsync_ReturnsSuccess_WhenPending()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var templateRequest = new TemplateRequest
            {
                Id = id,
                Status = TemplateRequestStatus.Pending,
                TemplateName = "Old Name"
            };
            var queryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);
            _messageService.Get("RecordUpdated").Returns("Updated.");

            var request = new UpdateTemplateRequest { TemplateName = "New Name" };
            var dto = new TemplateRequestDto { TemplateName = "New Name" };

            _mapper.When(x => x.Map(Arg.Any<UpdateTemplateRequest>(), Arg.Any<TemplateRequest>()))
                   .Do(x =>
                   {
                       var src = x.Arg<UpdateTemplateRequest>();
                       var dest = x.Arg<TemplateRequest>();
                       dest.TemplateName = src.TemplateName;
                   });

            _mapper.Map<TemplateRequestDto>(templateRequest).Returns(dto);

            var result = await _sut.UpdateRequestAsync(id, userId, request);

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("New Name", templateRequest.TemplateName);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }


        [Fact]
        public async Task CancelRequestAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            _currentTenant.TenantId.Returns((Guid?)null);
            _messageService.Get("Unauthorized").Returns("Unauthorized.");

            var result = await _sut.CancelRequestAsync(Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CancelRequestAsync_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            var id = Guid.NewGuid();
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            var emptyQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var result = await _sut.CancelRequestAsync(id);

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task CancelRequestAsync_ReturnsFail_WhenStatusIsNotPending()
        {
            var id = Guid.NewGuid();
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            var templateRequest = new TemplateRequest { Id = id, Status = TemplateRequestStatus.InProgress };
            var queryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();

            _templateRequestRepo.GetQueryable().Returns(queryable);
            _messageService.Get("TemplateRequestCannotBeCancelled").Returns("Cannot cancel.");

            var result = await _sut.CancelRequestAsync(id);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Cannot cancel.", result.Message);
        }

        [Fact]
        public async Task CancelRequestAsync_ReturnsSuccess_AndRefundsQuota_WhenPending()
        {
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var templateRequest = new TemplateRequest
            {
                Id = id,
                Status = TemplateRequestStatus.Pending
            };
            var queryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var activeSub = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30),
                CustomDesignRequestsUsed = 2
            };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { activeSub }.AsQueryable().BuildMock());

            _messageService.Get("TemplateRequestCancelled").Returns("Cancelled.");

            var result = await _sut.CancelRequestAsync(id);

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(TemplateRequestStatus.Cancelled, templateRequest.Status);
            Assert.Equal(1, activeSub.CustomDesignRequestsUsed);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
