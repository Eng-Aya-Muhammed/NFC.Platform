using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
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

        // ── GetByIdAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFound_WhenCardDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _cardRepo.GetByIdAsync(id).Returns((Card?)null);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.GetByIdAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsSuccess_WhenCardExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var card = new Card { Id = id, ActivationCode = "ABC" };
            var dto = new CardDto { Id = id };
            _cardRepo.GetByIdAsync(id).Returns(card);
            _mapper.Map<CardDto>(card).Returns(dto);

            // Act
            var result = await _sut.GetByIdAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Data!.Id);
        }

        // ── CreateCardAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task CreateCardAsync_ThrowsBusinessException_WhenCardCodeAlreadyExists()
        {
            // Arrange
            var request = new CreateCardRequest { ActivationCode = "DUPLICATE" };
            var existingCard = new Card { ActivationCode = "DUPLICATE" };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card> { existingCard });

            // Act & Assert
            await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateCardAsync(request));
        }

        [Fact]
        public async Task CreateCardAsync_ReturnsSuccess_WhenCodeIsUnique()
        {
            // Arrange
            var request = new CreateCardRequest { ActivationCode = "NEWCODE" };
            var card = new Card { ActivationCode = "NEWCODE" };
            var dto = new CardDto { ActivationCode = "NEWCODE" };

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card>());
            _mapper.Map<Card>(request).Returns(card);
            _mapper.Map<CardDto>(card).Returns(dto);
            _messageService.Get("RecordCreated").Returns("Record created.");

            // Act
            var result = await _sut.CreateCardAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("NEWCODE", result.Data!.ActivationCode);
            await _cardRepo.Received(1).AddAsync(card);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── ActivateCardAsync ─────────────────────────────────────────────────────

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
        public async Task ActivateCardAsync_ReturnsUnauthorized_WhenOnlyUserIdMissing()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var request = new ActivateCardRequest { ActivationCode = "CODE" };

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
            _currentTenant.UserId.Returns(Guid.NewGuid());
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card>());
            _messageService.Get("CardNotFound").Returns("Card not found.");

            // Act
            var result = await _sut.ActivateCardAsync(new ActivateCardRequest { ActivationCode = "GHOST" });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ActivateCardAsync_ReturnsBadRequest_WhenCardIsAlreadyActive()
        {
            // Arrange
            _currentTenant.UserId.Returns(Guid.NewGuid());
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var card = new Card { ActivationCode = "ACTIVE", Status = CardStatus.Active, UserProfileId = Guid.NewGuid() };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card> { card });
            _messageService.Get("CardAlreadyActivated").Returns("Card already activated");

            // Act
            var result = await _sut.ActivateCardAsync(new ActivateCardRequest { ActivationCode = "ACTIVE" });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Card already activated", result.Message);
        }

        [Fact]
        public async Task ActivateCardAsync_ReturnsBadRequest_WhenUserProfileLinkedButCardInactive()
        {
            // Arrange — card is inactive but already linked to a profile
            _currentTenant.UserId.Returns(Guid.NewGuid());
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var card = new Card { ActivationCode = "LINKED", Status = CardStatus.PendingGeneration, UserProfileId = Guid.NewGuid() };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card> { card });

            // Act
            var result = await _sut.ActivateCardAsync(new ActivateCardRequest { ActivationCode = "LINKED" });

            // Assert — UserProfileId != null triggers the already-activated guard
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ActivateCardAsync_ReturnsBadRequest_WhenUserNotFound_DuringProfileCreation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var card = new Card { ActivationCode = "VALID", Status = CardStatus.PendingGeneration, UserProfileId = null };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card> { card });

            // No existing profile
            _userProfileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile>());

            // User does not exist either
            _userRepo.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _sut.ActivateCardAsync(new ActivateCardRequest { ActivationCode = "VALID" });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ActivateCardAsync_Success_UsesExistingProfile_WhenProfileExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var card = new Card { ActivationCode = "VALID", Status = CardStatus.PendingGeneration, UserProfileId = null };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card> { card });

            var existingProfile = new UserProfile { Id = Guid.NewGuid(), UserId = userId };
            _userProfileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile> { existingProfile });

            _cardOrderItemRepo.FindAsync(Arg.Any<Expression<Func<CardOrderItem, bool>>>())
                .Returns(new List<CardOrderItem>());

            _messageService.Get("CardActivatedSuccessfully").Returns("Card activated successfully.");

            // Act
            var result = await _sut.ActivateCardAsync(new ActivateCardRequest { ActivationCode = "VALID" });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(card.IsActive);
            Assert.Equal(existingProfile.Id, card.UserProfileId);
            // Should NOT create a new profile
            await _userProfileRepo.DidNotReceive().AddAsync(Arg.Any<UserProfile>());
        }

        [Fact]
        public async Task ActivateCardAsync_Success_CreatesProfileAndLinksOrderItem_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var card = new Card { ActivationCode = "VALID_CODE", Status = CardStatus.PendingGeneration, UserProfileId = null };
            var user = new User { Id = userId, Username = "testuser" };
            var orderItem = new CardOrderItem { ActivationCode = "VALID_CODE", LinkedCardId = null };

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>())
                .Returns(new List<Card> { card });
            _userProfileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile>());
            _userRepo.GetByIdAsync(userId).Returns(user);
            _cardOrderItemRepo.FindAsync(Arg.Any<Expression<Func<CardOrderItem, bool>>>())
                .Returns(new List<CardOrderItem> { orderItem });
            _messageService.Get("CardActivatedSuccessfully").Returns("Card activated successfully.");

            // Act
            var result = await _sut.ActivateCardAsync(new ActivateCardRequest { ActivationCode = "VALID_CODE" });

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(card.IsActive);
            Assert.NotNull(card.ActivatedAt);
            await _userProfileRepo.Received(1).AddAsync(Arg.Any<UserProfile>());
        }

        // ── DeleteCardAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteCardAsync_ReturnsNotFound_WhenCardDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _cardRepo.GetByIdAsync(id).Returns((Card?)null);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.DeleteCardAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task DeleteCardAsync_ReturnsSuccess_AndRemovesCard()
        {
            // Arrange
            var id = Guid.NewGuid();
            var card = new Card { Id = id };
            _cardRepo.GetByIdAsync(id).Returns(card);
            _messageService.Get("RecordDeleted").Returns("Record deleted.");

            // Act
            var result = await _sut.DeleteCardAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            _cardRepo.Received(1).Remove(card);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
