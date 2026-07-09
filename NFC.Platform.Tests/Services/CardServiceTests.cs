using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
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
    public class CardServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        private readonly IGenericRepository<Card> _cardRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;
        private readonly IGenericRepository<CardOrderItem> _cardOrderItemRepo;

        private readonly CardService _sut;

        public CardServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _cardRepo = Substitute.For<IGenericRepository<Card>>();
            _userRepo = Substitute.For<IGenericRepository<User>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _cardOrderItemRepo = Substitute.For<IGenericRepository<CardOrderItem>>();

            _unitOfWork.Repository<Card>().Returns(_cardRepo);
            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);
            _unitOfWork.Repository<CardOrderItem>().Returns(_cardOrderItemRepo);

            _sut = new CardService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        [Fact]
        public async Task ActivateCardAsync_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);
            _currentTenant.TenantId.Returns((Guid?)null);

            var request = new ActivateCardRequest { ActivationCode = "123456" };

            // Act
            var result = await _sut.ActivateCardAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task ActivateCardAsync_ReturnsNotFound_WhenCardDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(tenantId);

            var request = new ActivateCardRequest { ActivationCode = "NONEXISTENT" };

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card>());
            _messageService.Get("CardNotFound").Returns("Card not found.");

            // Act
            var result = await _sut.ActivateCardAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ActivateCardAsync_ReturnsBadRequest_WhenCardIsAlreadyActive()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(tenantId);

            var request = new ActivateCardRequest { ActivationCode = "ACTIVE_CODE" };

            var existingCard = new Card
            {
                ActivationCode = "ACTIVE_CODE",
                IsActive = true,
                UserProfileId = Guid.NewGuid()
            };

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card> { existingCard });

            // Act
            var result = await _sut.ActivateCardAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Card already activated", result.Message);
        }

        [Fact]
        public async Task ActivateCardAsync_ReturnsSuccess_CreatesProfileAndLinksOrderItem_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(tenantId);

            var request = new ActivateCardRequest { ActivationCode = "VALID_CODE" };

            var card = new Card
            {
                ActivationCode = "VALID_CODE",
                IsActive = false,
                UserProfileId = null,
                TenantId = tenantId
            };

            var user = new User
            {
                Id = userId,
                Username = "testuser",
                TenantId = tenantId
            };

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card> { card });

            _userProfileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile>()); // No existing profile

            _userRepo.GetByIdAsync(userId).Returns(user);

            var orderItem = new CardOrderItem
            {
                ActivationCode = "VALID_CODE",
                EmployeeName = "testuser",
                LinkedCardId = null
            };

            _cardOrderItemRepo.FindAsync(Arg.Any<Expression<Func<CardOrderItem, bool>>>())
                .Returns(new List<CardOrderItem> { orderItem });

            _messageService.Get("CardActivatedSuccessfully").Returns("Card activated successfully.");

            // Act
            var result = await _sut.ActivateCardAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(card.IsActive);
            Assert.NotNull(card.UserProfileId);
            Assert.Equal(tenantId, card.TenantId);
            Assert.Equal(card.Id, orderItem.LinkedCardId);

            await _userProfileRepo.Received(1).AddAsync(Arg.Any<UserProfile>());
            _cardRepo.Received(1).Update(card);
            _cardOrderItemRepo.Received(1).Update(orderItem);
            await _unitOfWork.Received(2).SaveChangesAsync(); // 1. Save new profile, 2. Save card + order items together
        }
    }
}
