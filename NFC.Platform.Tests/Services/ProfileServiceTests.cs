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

        private readonly ProfileService _sut;

        public ProfileServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();

            _userRepo = Substitute.For<IGenericRepository<User>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();

            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);

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

        // ── SynchronizeLinksAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task SynchronizeLinksAsync_AddsNewCustomLinks_WhileIgnoringStandardPlatforms()
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
            Assert.Equal(2, userProfile.CustomLinks.Count);
            Assert.Contains(userProfile.CustomLinks, l => l.Url == "https://custom1.com");
            Assert.Contains(userProfile.CustomLinks, l => l.Url == "https://custom2.com");
            Assert.DoesNotContain(userProfile.CustomLinks, l => l.Url == PlatformConstants.LinkedIn);

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
    }
}
