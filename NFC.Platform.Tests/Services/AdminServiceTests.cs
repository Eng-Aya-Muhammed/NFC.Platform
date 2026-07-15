using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.Application.Interfaces.Repositories;
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

        private readonly IGenericRepository<CardOrder> _orderRepo;
        private readonly IGenericRepository<TemplateRequest> _templateRequestRepo;
        private readonly IGenericRepository<CardTemplate> _cardTemplateRepo;
        private readonly IGenericRepository<Tenant> _tenantRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;

        private readonly AdminService _sut;

        public AdminServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();

            _orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            _templateRequestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();
            _cardTemplateRepo = Substitute.For<IGenericRepository<CardTemplate>>();
            _tenantRepo = Substitute.For<IGenericRepository<Tenant>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<TemplateRequest>().Returns(_templateRequestRepo);
            _unitOfWork.Repository<CardTemplate>().Returns(_cardTemplateRepo);
            _unitOfWork.Repository<Tenant>().Returns(_tenantRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);

            _sut = new AdminService(_unitOfWork, _mapper, _messageService);
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
    }
}
