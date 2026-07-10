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
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CardTemplateServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        private readonly IGenericRepository<CardTemplate> _cardTemplateRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;
        private readonly IGenericRepository<User> _userRepo;

        private readonly CardTemplateService _sut;

        public CardTemplateServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();

            _cardTemplateRepo = Substitute.For<IGenericRepository<CardTemplate>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _userRepo = Substitute.For<IGenericRepository<User>>();

            _unitOfWork.Repository<CardTemplate>().Returns(_cardTemplateRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);
            _unitOfWork.Repository<User>().Returns(_userRepo);

            _sut = new CardTemplateService(_unitOfWork, _mapper, _messageService);
        }

        [Fact]
        public async Task GetActiveTemplatesAsync_ReturnsActiveTemplatesSortedByDisplayOrder()
        {
            // Arrange
            var templates = new List<CardTemplate>
            {
                new CardTemplate { Name = "Second", IsActive = true, DisplayOrder = 2 },
                new CardTemplate { Name = "First", IsActive = true, DisplayOrder = 1 },
                new CardTemplate { Name = "Inactive", IsActive = false, DisplayOrder = 0 }
            };

            var mock = templates.BuildMock();
            _cardTemplateRepo.GetQueryable().Returns(mock);

            var dtos = new List<CardTemplateDto>
            {
                new CardTemplateDto { Name = "First", DisplayOrder = 1 },
                new CardTemplateDto { Name = "Second", DisplayOrder = 2 }
            };

            _mapper.Map<IReadOnlyList<CardTemplateDto>>(Arg.Any<List<CardTemplate>>())
                .Returns(dtos);

            // Act
            var result = await _sut.GetActiveTemplatesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal("First", result.Data[0].Name);
        }

        [Fact]
        public async Task SelectTemplateAsync_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            // Arrange
            _cardTemplateRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((CardTemplate?)null);
            _messageService.Get("RecordNotFound").Returns("Template not found.");

            // Act
            var result = await _sut.SelectTemplateAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task SelectTemplateAsync_ReturnsBadRequest_WhenTemplateIsInactive()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            var inactiveTemplate = new CardTemplate { Id = templateId, IsActive = false };
            _cardTemplateRepo.GetByIdAsync(templateId).Returns(inactiveTemplate);

            // Act
            var result = await _sut.SelectTemplateAsync(Guid.NewGuid(), templateId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Template is inactive and cannot be selected.", result.Message);
        }

        [Fact]
        public async Task SelectTemplateAsync_Success_UpdatesUserProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var activeTemplate = new CardTemplate { Id = templateId, IsActive = true };
            var profile = new UserProfile { UserId = userId };

            _cardTemplateRepo.GetByIdAsync(templateId).Returns(activeTemplate);
            _userProfileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile> { profile });

            _messageService.Get("RecordUpdated").Returns("Template selected successfully.");

            // Act
            var result = await _sut.SelectTemplateAsync(userId, templateId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(templateId, profile.CardTemplateId);
            _userProfileRepo.Received(1).Update(profile);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task SelectTemplateAsync_CreatesNewProfile_WhenUserProfileIsNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var activeTemplate = new CardTemplate { Id = templateId, IsActive = true };
            var user = new User { Id = userId, TenantId = tenantId, Username = "testuser" };

            _cardTemplateRepo.GetByIdAsync(templateId).Returns(activeTemplate);
            _userProfileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile>());

            _userRepo.GetByIdAsync(userId).Returns(user);
            _messageService.Get("RecordUpdated").Returns("Template selected successfully.");

            // Act
            var result = await _sut.SelectTemplateAsync(userId, templateId);

            // Assert
            Assert.True(result.IsSuccess);
            await _userProfileRepo.Received(1).AddAsync(Arg.Is<UserProfile>(p =>
                p.UserId == userId &&
                p.TenantId == tenantId &&
                p.FullName == "testuser" &&
                p.CardTemplateId == templateId
            ));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
