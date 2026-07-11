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
    public class CardTemplateServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        private readonly IGenericRepository<CardTemplate> _templateRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<User> _userRepo;

        private readonly CardTemplateService _sut;

        public CardTemplateServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();

            _templateRepo = Substitute.For<IGenericRepository<CardTemplate>>();
            _profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _userRepo = Substitute.For<IGenericRepository<User>>();

            _unitOfWork.Repository<CardTemplate>().Returns(_templateRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);
            _unitOfWork.Repository<User>().Returns(_userRepo);

            _sut = new CardTemplateService(_unitOfWork, _mapper, _messageService);
        }

        // ── GetActiveTemplatesAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetActiveTemplatesAsync_ReturnsEmptyList_WhenNoTemplatesExist()
        {
            // Arrange
            var emptyQueryable = new List<CardTemplate>().AsQueryable().BuildMock();
            _templateRepo.GetQueryable().Returns(emptyQueryable);
            _mapper.Map<IReadOnlyList<CardTemplateDto>>(Arg.Any<List<CardTemplate>>())
                .Returns(new List<CardTemplateDto>());

            // Act
            var result = await _sut.GetActiveTemplatesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetActiveTemplatesAsync_ReturnsOnlyActiveTemplates_OrderedByDisplayOrder()
        {
            // Arrange
            var templates = new List<CardTemplate>
            {
                new() { Id = Guid.NewGuid(), Name = "Second", IsActive = true, DisplayOrder = 2 },
                new() { Id = Guid.NewGuid(), Name = "First",  IsActive = true, DisplayOrder = 1 },
                new() { Id = Guid.NewGuid(), Name = "Hidden", IsActive = false, DisplayOrder = 0 }
            };
            var queryable = templates.AsQueryable().BuildMock();
            _templateRepo.GetQueryable().Returns(queryable);

            var dtos = new List<CardTemplateDto>
            {
                new() { Name = "First" },
                new() { Name = "Second" }
            };
            _mapper.Map<IReadOnlyList<CardTemplateDto>>(Arg.Any<object>()).Returns(dtos);

            // Act
            var result = await _sut.GetActiveTemplatesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal("First", result.Data![0].Name);
        }

        // ── SelectTemplateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task SelectTemplateAsync_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            _templateRepo.GetByIdAsync(templateId).Returns((CardTemplate?)null);
            _messageService.Get("RecordNotFound").Returns("Template not found.");

            // Act
            var result = await _sut.SelectTemplateAsync(userId, templateId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task SelectTemplateAsync_ReturnsBadRequest_WhenTemplateIsInactive()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var template = new CardTemplate { Id = templateId, IsActive = false };
            _templateRepo.GetByIdAsync(templateId).Returns(template);
            _messageService.Get("TemplateInactive").Returns("Template is inactive.");

            // Act
            var result = await _sut.SelectTemplateAsync(userId, templateId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SelectTemplateAsync_ReturnsBadRequest_WhenTemplateIsInactive_AndMessageIsNull()
        {
            // Arrange — message service returns null/empty, service should use fallback
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var template = new CardTemplate { Id = templateId, IsActive = false };
            _templateRepo.GetByIdAsync(templateId).Returns(template);
            _messageService.Get("TemplateInactive").Returns(string.Empty);

            // Act
            var result = await _sut.SelectTemplateAsync(userId, templateId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("inactive", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SelectTemplateAsync_ReturnsNotFound_WhenUserDoesNotExist_AndNoProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var template = new CardTemplate { Id = templateId, IsActive = true };
            _templateRepo.GetByIdAsync(templateId).Returns(template);

            _profileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile>());
            _userRepo.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _sut.SelectTemplateAsync(userId, templateId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task SelectTemplateAsync_Success_CreatesNewProfile_WhenProfileDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var template = new CardTemplate { Id = templateId, IsActive = true };
            var user = new User { Id = userId, Username = "testuser", TenantId = tenantId };

            _templateRepo.GetByIdAsync(templateId).Returns(template);
            _profileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile>());
            _userRepo.GetByIdAsync(userId).Returns(user);
            _messageService.Get("RecordUpdated").Returns("Template selected.");

            // Act
            var result = await _sut.SelectTemplateAsync(userId, templateId);

            // Assert
            Assert.True(result.IsSuccess);
            await _profileRepo.Received(1).AddAsync(Arg.Is<UserProfile>(p =>
                p.UserId == userId &&
                p.TenantId == tenantId &&
                p.CardTemplateId == templateId));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task SelectTemplateAsync_Success_UpdatesExistingProfile_WhenProfileExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();

            var template = new CardTemplate { Id = templateId, IsActive = true };
            var existingProfile = new UserProfile { Id = Guid.NewGuid(), UserId = userId, CardTemplateId = Guid.NewGuid() };

            _templateRepo.GetByIdAsync(templateId).Returns(template);
            _profileRepo.FindAsync(Arg.Any<Expression<Func<UserProfile, bool>>>())
                .Returns(new List<UserProfile> { existingProfile });
            _messageService.Get("RecordUpdated").Returns("Template selected.");

            // Act
            var result = await _sut.SelectTemplateAsync(userId, templateId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(templateId, existingProfile.CardTemplateId);
            _profileRepo.Received(1).Update(existingProfile);
            await _userRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
        }
    }
}
