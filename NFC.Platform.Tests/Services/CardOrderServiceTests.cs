using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CardOrderServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        private readonly IGenericRepository<CardOrder> _orderRepo;
        private readonly IGenericRepository<Card> _cardRepo;
        private readonly IGenericRepository<CardOrderItem> _orderItemRepo;

        private readonly CardOrderService _sut;

        public CardOrderServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            _cardRepo = Substitute.For<IGenericRepository<Card>>();
            _orderItemRepo = Substitute.For<IGenericRepository<CardOrderItem>>();

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<Card>().Returns(_cardRepo);
            _unitOfWork.Repository<CardOrderItem>().Returns(_orderItemRepo);

            _sut = new CardOrderService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var emptyQueryable = new List<CardOrder>().AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.GetByIdAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsSuccess_WhenOrderExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var order = new CardOrder { Id = id, Items = [] };
            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);
            var dto = new CardOrderDto { Id = id };
            _mapper.Map<CardOrderDto>(order).Returns(dto);

            // Act
            var result = await _sut.GetByIdAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(id, result.Data!.Id);
        }

        // ── CreateAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);
            var request = new CreateCardOrderRequest { Quantity = 10 };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateAsync_ReturnsSuccess_AndSetsDefaults_WhenNoCardNameOrType()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var request = new CreateCardOrderRequest { Quantity = 5 };
            var order = new CardOrder { Id = Guid.NewGuid(), Quantity = 5, Items = [] };
            _mapper.Map<CardOrder>(request).Returns(order);

            var createdQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(createdQueryable);

            var dto = new CardOrderDto { Quantity = 5 };
            _mapper.Map<CardOrderDto>(order).Returns(dto);
            _messageService.Get("RecordCreated").Returns("Record created.");

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            // Defaults should have been applied to the order object
            Assert.Equal($"طلب كروت - 5", order.CardName);
            Assert.Equal(CardType.Plastic, order.CardType);
            Assert.Equal(CardDesignType.BuiltInTemplate, order.CardDesignType);
            await _orderRepo.Received(1).AddAsync(order);
        }

        // ── UpdateStatusAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateStatusAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _orderRepo.GetByIdAsync(id).Returns((CardOrder?)null);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.UpdateStatusAsync(id, new UpdateCardOrderStatusRequest { Status = OrderStatus.UnderReview });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateStatusAsync_ReturnsSuccess_AndUpdatesStatus()
        {
            // Arrange
            var id = Guid.NewGuid();
            var order = new CardOrder { Id = id, Status = OrderStatus.Pending };
            _orderRepo.GetByIdAsync(id).Returns(order);
            _messageService.Get("RecordUpdated").Returns("Record updated.");

            // Act
            var result = await _sut.UpdateStatusAsync(id, new UpdateCardOrderStatusRequest { Status = OrderStatus.UnderReview });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(OrderStatus.UnderReview, order.Status);
            _orderRepo.Received(1).Update(order);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── DeleteAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _orderRepo.GetByIdAsync(id).Returns((CardOrder?)null);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.DeleteAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsSuccess_AndRemovesOrder()
        {
            // Arrange
            var id = Guid.NewGuid();
            var order = new CardOrder { Id = id };
            _orderRepo.GetByIdAsync(id).Returns(order);
            _messageService.Get("RecordDeleted").Returns("Record deleted.");

            // Act
            var result = await _sut.DeleteAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            _orderRepo.Received(1).Remove(order);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── AssignCardsAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task AssignCardsAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var emptyQueryable = new List<CardOrder>().AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var request = new AssignCardsRequest();

            // Act
            var result = await _sut.AssignCardsAsync(orderId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task AssignCardsAsync_SkipsAssignment_WhenOrderItemNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = orderId,
                TenantId = Guid.NewGuid(),
                Items = [] // empty — no matching items
            };

            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>()).Returns(new List<Card>());
            _messageService.Get("RecordUpdated").Returns("Updated.");

            var request = new AssignCardsRequest
            {
                Assignments =
                [
                    new CardAssignmentDto { OrderItemId = Guid.NewGuid(), ActivationCode = "GHOST" }
                ]
            };

            // Act
            var result = await _sut.AssignCardsAsync(orderId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _cardRepo.DidNotReceive().AddAsync(Arg.Any<Card>());
        }

        [Fact]
        public async Task AssignCardsAsync_ReuseExistingCard_WhenCardAlreadyExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var orderItemId = Guid.NewGuid();
            var existingCardId = Guid.NewGuid();

            var orderItem = new CardOrderItem { Id = orderItemId, UserProfileId = null };
            var order = new CardOrder
            {
                Id = orderId,
                TenantId = tenantId,
                Items = [orderItem]
            };

            var existingCard = new Card { Id = existingCardId, ActivationCode = "EXISTING_CODE", CardOrderId = Guid.NewGuid() };

            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>()).Returns(new List<Card> { existingCard });
            _messageService.Get("RecordUpdated").Returns("Updated.");

            var request = new AssignCardsRequest
            {
                Assignments =
                [
                    new CardAssignmentDto { OrderItemId = orderItemId, ActivationCode = "EXISTING_CODE" }
                ]
            };

            // Act
            var result = await _sut.AssignCardsAsync(orderId, request);

            // Assert
            Assert.True(result.IsSuccess);
            // Should NOT add a new card — it reused the existing one
            await _cardRepo.DidNotReceive().AddAsync(Arg.Any<Card>());
            Assert.Equal(orderId, existingCard.CardOrderId);
        }

        [Fact]
        public async Task AssignCardsAsync_Success_CreatesCardsAndLinksToEmployeeProfiles()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var orderItemId1 = Guid.NewGuid();
            var orderItemId2 = Guid.NewGuid();
            var userProfileId = Guid.NewGuid();

            var order = new CardOrder
            {
                Id = orderId,
                TenantId = tenantId,
                Items =
                [
                    new CardOrderItem { Id = orderItemId1, UserProfileId = userProfileId },
                    new CardOrderItem { Id = orderItemId2, UserProfileId = null }
                ]
            };

            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>()).Returns(new List<Card>());
            _messageService.Get("RecordUpdated").Returns("Record updated successfully.");

            var request = new AssignCardsRequest
            {
                Assignments =
                [
                    new CardAssignmentDto { OrderItemId = orderItemId1, ActivationCode = "CODE1" },
                    new CardAssignmentDto { OrderItemId = orderItemId2, ActivationCode = "CODE2" }
                ]
            };

            // Act
            var result = await _sut.AssignCardsAsync(orderId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _unitOfWork.Received(1).BeginTransactionAsync();

            var item1 = order.Items.First(i => i.Id == orderItemId1);
            Assert.Equal("CODE1", item1.ActivationCode);

            var item2 = order.Items.First(i => i.Id == orderItemId2);
            Assert.Equal("CODE2", item2.ActivationCode);

            await _cardRepo.Received(1).AddAsync(Arg.Is<Card>(c => c.ActivationCode == "CODE1" && c.UserProfileId == userProfileId && c.IsActive));
            await _cardRepo.Received(1).AddAsync(Arg.Is<Card>(c => c.ActivationCode == "CODE2" && c.UserProfileId == null && !c.IsActive));
            await _unitOfWork.Received(1).CommitTransactionAsync();
        }
    }
}
