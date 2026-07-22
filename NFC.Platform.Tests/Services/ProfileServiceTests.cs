using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.Constants;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.DTOs.Profile;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class ProfileServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;
        private readonly IGenericRepository<CardTemplate> _cardTemplateRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;

        private readonly ProfileService _sut;

        public ProfileServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();

            _userRepo = Substitute.For<IGenericRepository<User>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _cardTemplateRepo = Substitute.For<IGenericRepository<CardTemplate>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();

            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);
            _unitOfWork.Repository<CardTemplate>().Returns(_cardTemplateRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);

            _sut = new ProfileService(_unitOfWork, _mapper, _messageService);
        }

        // ── GetProfileAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetProfileAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var mockQuery = new List<User>().AsQueryable().BuildMock();
            _userRepo.GetQueryable().Returns(mockQuery);

            // Act
            var result = await _sut.GetProfileAsync(Guid.NewGuid());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsProfile_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserProfile = new UserProfile { FullName = "John Doe" } };
            var mockQuery = new List<User> { user }.AsQueryable().BuildMock();
            _userRepo.GetQueryable().Returns(mockQuery);

            var expectedDto = new EmployeeDetailsDto { FullName = "John Doe" };
            _mapper.Map<EmployeeDetailsDto>(user).Returns(expectedDto);

            // Act
            var result = await _sut.GetProfileAsync(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("John Doe", result.Data!.FullName);
        }

        // ── UpdateProfileAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateProfileAsync_UpdatesUserProfileFields()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserProfile = new UserProfile { FullName = "Old Name" } };
            var mockQuery = new List<User> { user }.AsQueryable().BuildMock();
            _userRepo.GetQueryable().Returns(mockQuery);

            var request = new UpdateMyProfileRequest { FullName = "New Name" };
            _mapper.Map(request, user.UserProfile).Returns(user.UserProfile);
            _mapper.Map<EmployeeDetailsDto>(user).Returns(new EmployeeDetailsDto { FullName = "New Name" });

            // Act
            var result = await _sut.UpdateProfileAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateProfileAsync_AlsoUpdatesCustomLinks_WhenLinksAreProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userProfile = new UserProfile { Id = Guid.NewGuid(), FullName = "Old Name", CustomLinks = [] };
            var user = new User { Id = userId, UserProfile = userProfile };
            var mockQuery = new List<User> { user }.AsQueryable().BuildMock();
            _userRepo.GetQueryable().Returns(mockQuery);

            var request = new UpdateMyProfileRequest 
            { 
                FullName = "New Name",
                Links = [ new NFC.Platform.Application.DTOs.Profile.CustomLinkInput { Title = "Website", Url = "https://example.com" } ]
            };

            _mapper.Map(request, user.UserProfile).Returns(user.UserProfile);
            _mapper.Map<EmployeeDetailsDto>(user).Returns(new EmployeeDetailsDto { FullName = "New Name" });

            // Act
            var result = await _sut.UpdateProfileAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(userProfile.CustomLinks);
            Assert.Equal("https://example.com", userProfile.CustomLinks.First().Url);
        }

        // ── SynchronizeLinksAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task SynchronizeLinksAsync_AddsAllLinksAsCustom()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userProfile = new UserProfile
            {
                FullName = "ACME User",
                CustomLinks = new List<ProfileLink>()
            };
            var user = new User { Id = userId, UserProfile = userProfile };
            var mockQuery = new List<User> { user }.AsQueryable().BuildMock();
            _userRepo.GetQueryable().Returns(mockQuery);

            var request = new SynchronizeLinksRequest
            {
                Links = ["https://custom1.com", PlatformConstants.LinkedIn, "https://custom2.com"]
            };

            // Act
            var result = await _sut.SynchronizeLinksAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, userProfile.CustomLinks.Count);
            Assert.Contains(userProfile.CustomLinks, l => l.Url == "https://custom1.com");
            Assert.Contains(userProfile.CustomLinks, l => l.Url == "https://custom2.com");
            Assert.Contains(userProfile.CustomLinks, l => l.Url == PlatformConstants.LinkedIn);

            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateProfileAsync_CreatesNewProfile_WhenUserProfileIsNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, TenantId = Guid.NewGuid(), UserProfile = null };
            var mockQuery = new List<User> { user }.AsQueryable().BuildMock();
            _userRepo.GetQueryable().Returns(mockQuery);

            var request = new UpdateMyProfileRequest { FullName = "New User" };
            _mapper.Map<EmployeeDetailsDto>(user).Returns(new EmployeeDetailsDto { FullName = "New User" });

            // Act
            var result = await _sut.UpdateProfileAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(user.UserProfile);
            await _userProfileRepo.Received(1).AddAsync(Arg.Any<UserProfile>());
            await _unitOfWork.Received(2).SaveChangesAsync(); // One for add, one for map/save
        }

        [Fact]
        public async Task UpdateProfileAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepo.GetQueryable().Returns(new List<User>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.UpdateProfileAsync(userId, new UpdateMyProfileRequest());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateProfileAsync_ReturnsConflict_WhenSubdomainAlreadyTaken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User 
            { 
                Id = userId, 
                TenantId = Guid.NewGuid(), 
                UserProfile = new UserProfile { Id = Guid.NewGuid(), Subdomain = "old-subdomain" }
            };
            var mockQuery = new List<User> { user }.AsQueryable().BuildMock();
            _userRepo.GetQueryable().Returns(mockQuery);

            var anotherProfile = new UserProfile { Id = Guid.NewGuid(), Subdomain = "taken-subdomain" };
            var profiles = new List<UserProfile> { anotherProfile }.AsQueryable().BuildMock();
            _userProfileRepo.GetQueryable().Returns(profiles);

            _messageService.Get("SubdomainAlreadyTaken").Returns("The requested subdomain is already in use.");

            var request = new UpdateMyProfileRequest { Subdomain = "taken-subdomain" };

            // Act
            var result = await _sut.UpdateProfileAsync(userId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("The requested subdomain is already in use.", result.Message);
        }

        [Fact]
        public async Task SynchronizeLinksAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepo.GetQueryable().Returns(new List<User>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.SynchronizeLinksAsync(userId, new SynchronizeLinksRequest { Links = [] });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task SynchronizeLinksAsync_CreatesNewProfile_WhenUserProfileIsNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, TenantId = Guid.NewGuid(), UserProfile = null };
            var mockQuery = new List<User> { user }.AsQueryable().BuildMock();
            _userRepo.GetQueryable().Returns(mockQuery);

            var request = new SynchronizeLinksRequest { Links = new List<string> { "https://test.com" } };

            // Act
            var result = await _sut.SynchronizeLinksAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(user.UserProfile);
            await _userProfileRepo.Received(1).AddAsync(Arg.Any<UserProfile>());
            await _unitOfWork.Received(2).SaveChangesAsync();
        }

        [Fact]
        public async Task SynchronizeLinksAsync_ThrowsArgumentNull_WhenRequestIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.SynchronizeLinksAsync(Guid.NewGuid(), null!));
        }

        [Fact]
        public async Task SynchronizeLinksAsync_HandlesEmptyLinksArray()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var profile = new UserProfile { CustomLinks = new List<ProfileLink> { new ProfileLink { Url = "https://old.com" } } };
            var user = new User { Id = userId, UserProfile = profile };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());

            var request = new SynchronizeLinksRequest { Links = [] };

            // Act
            var result = await _sut.SynchronizeLinksAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(profile.CustomLinks);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateProfileTemplateAsync_TemplateDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var request = templateId;

            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate>().AsQueryable().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Not found");

            // Act
            var result = await _sut.UpdateProfileTemplateAsync(userId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateProfileTemplateAsync_SubscriptionExpired_ReturnsFail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var request = templateId;

            var template = new CardTemplate { Id = templateId, IsActive = true, IsDeleted = false };
            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate> { template }.AsQueryable().BuildMock());

            var user = new User { Id = userId, TenantId = tenantId, UserProfile = new UserProfile() };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());

            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription>().AsQueryable().BuildMock());
            _messageService.Get("SubscriptionExpiredOrMissing").Returns("Missing sub");

            // Act
            var result = await _sut.UpdateProfileTemplateAsync(userId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Missing sub", result.Message);
        }

        [Fact]
        public async Task UpdateProfileTemplateAsync_TemplateNotAllowed_ReturnsFail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var request = templateId;

            var template = new CardTemplate { Id = templateId, IsActive = true, IsDeleted = false };
            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate> { template }.AsQueryable().BuildMock());

            var user = new User { Id = userId, TenantId = tenantId, UserProfile = new UserProfile() };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());

            var sub = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30),
                SubscriptionPlan = new SubscriptionPlan
                {
                    PlanTemplates = new List<SubscriptionPlanTemplate>() // Empty = no templates allowed
                }
            };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { sub }.AsQueryable().BuildMock());
            _messageService.Get("TemplateNotAllowedInPlan").Returns("Not allowed");

            // Act
            var result = await _sut.UpdateProfileTemplateAsync(userId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task UpdateProfileTemplateAsync_LimitReached_ReturnsFail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var request = templateId;

            var template = new CardTemplate { Id = templateId, IsActive = true, IsDeleted = false };
            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate> { template }.AsQueryable().BuildMock());

            var user = new User { Id = userId, TenantId = tenantId, UserProfile = new UserProfile() };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());

            var sub = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30),
                TemplateChangesUsed = 5,
                SubscriptionPlan = new SubscriptionPlan
                {
                    MaxTemplateChanges = 5,
                    PlanTemplates = new List<SubscriptionPlanTemplate> { new() { CardTemplateId = templateId } }
                }
            };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { sub }.AsQueryable().BuildMock());
            _messageService.Get("TemplateChangeLimitReached").Returns("Limit reached");

            // Act
            var result = await _sut.UpdateProfileTemplateAsync(userId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UpdateProfileTemplateAsync_Valid_UpdatesTemplateAndIncrementsCounter()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var request = templateId;

            var template = new CardTemplate { Id = templateId, IsActive = true, IsDeleted = false };
            _cardTemplateRepo.GetQueryable().Returns(new List<CardTemplate> { template }.AsQueryable().BuildMock());

            var profile = new UserProfile { UserId = userId, ProfileTemplateId = null };
            var user = new User { Id = userId, TenantId = tenantId, UserProfile = profile };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());

            var sub = new UserSubscription
            {
                TenantId = tenantId,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30),
                TemplateChangesUsed = 2,
                SubscriptionPlan = new SubscriptionPlan
                {
                    MaxTemplateChanges = 5,
                    PlanTemplates = new List<SubscriptionPlanTemplate> { new() { CardTemplateId = templateId } }
                }
            };
            _subscriptionRepo.GetQueryable().Returns(new List<UserSubscription> { sub }.AsQueryable().BuildMock());
            _mapper.Map<EmployeeDetailsDto>(user).Returns(new EmployeeDetailsDto { Id = userId });
            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            // Act
            var result = await _sut.UpdateProfileTemplateAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(templateId, profile.ProfileTemplateId);
            Assert.Equal(3, sub.TemplateChangesUsed); // Incremented
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
