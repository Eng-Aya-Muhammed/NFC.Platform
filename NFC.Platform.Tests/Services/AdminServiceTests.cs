using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.Application.DTOs.Upload;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

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

        private readonly AdminService _sut;

        public AdminServiceTests()
        {
            _unitOfWork          = Substitute.For<IUnitOfWork>();
            _mapper              = Substitute.For<IMapper>();
            _messageService      = Substitute.For<IMessageService>();

            _storageService      = Substitute.For<IStorageService>();

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

            _sut = new AdminService(_unitOfWork, _mapper, _messageService, _storageService);
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
            _templateRequestRepo.GetByIdAsync(requestId).Returns(request);

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

        [Fact]
        public async Task DeleteTemplateAsync_TogglesIsActiveStatus()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            var template = new CardTemplate { Id = templateId, IsActive = true };
            _cardTemplateRepo.GetByIdAsync(templateId).Returns(template);

            // Act
            var result = await _sut.DeleteTemplateAsync(templateId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(template.IsActive);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

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
            _templateRequestRepo.GetByIdAsync(id).Returns((TemplateRequest?)null);

            // Act
            var result = await _sut.ResolveTemplateRequestAsync(id, new ResolveTemplateRequestDto());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
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

    }
}
