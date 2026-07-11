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
                Items = new List<CardOrderItem>
                {
                    new CardOrderItem { Id = orderItemId1, UserProfileId = userProfileId },
                    new CardOrderItem { Id = orderItemId2, UserProfileId = null } // bulk/excel item
                }
            };

            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);

            // Mock Card repository FindAsync to return empty list (simulating new cards)
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>()).Returns(new List<Card>());
            _messageService.Get("RecordUpdated").Returns("Record updated successfully.");

            var request = new AssignCardsRequest
            {
                Assignments = new List<CardAssignmentDto>
                {
                    new CardAssignmentDto { OrderItemId = orderItemId1, ActivationCode = "CODE1" },
                    new CardAssignmentDto { OrderItemId = orderItemId2, ActivationCode = "CODE2" }
                }
            };

            // Act
            var result = await _sut.AssignCardsAsync(orderId, request);

            // Assert
            Assert.True(result.IsSuccess);

            await _unitOfWork.Received(1).BeginTransactionAsync();

            // Assert items were updated with ActivationCode
            var item1 = order.Items.First(i => i.Id == orderItemId1);
            Assert.Equal("CODE1", item1.ActivationCode);

            var item2 = order.Items.First(i => i.Id == orderItemId2);
            Assert.Equal("CODE2", item2.ActivationCode);

            // Assert cards were added
            await _cardRepo.Received(1).AddAsync(Arg.Is<Card>(c => c.ActivationCode == "CODE1" && c.UserProfileId == userProfileId && c.IsActive));
            await _cardRepo.Received(1).AddAsync(Arg.Is<Card>(c => c.ActivationCode == "CODE2" && c.UserProfileId == null && !c.IsActive));

            await _unitOfWork.Received(1).CommitTransactionAsync();
        }
    }
}
