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
    public class ProfileMetricServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;

        private readonly IGenericRepository<Card> _cardRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<ProfileMetric> _metricRepo;

        private readonly ProfileMetricService _sut;

        public ProfileMetricServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _mapper = Substitute.For<IMapper>();

            _cardRepo = Substitute.For<IGenericRepository<Card>>();
            _profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _metricRepo = Substitute.For<IGenericRepository<ProfileMetric>>();

            _unitOfWork.Repository<Card>().Returns(_cardRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);
            _unitOfWork.Repository<ProfileMetric>().Returns(_metricRepo);

            _sut = new ProfileMetricService(_unitOfWork, _messageService, _mapper);
        }

        // ── ResolvePublicProfileAsync ─────────────────────────────────────────────

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenActivationCodeIsNull()
        {
            // Arrange
            _messageService.Get("CardNotFound").Returns("Card not found.");

            // Act
            var result = await _sut.ResolvePublicProfileAsync(null!);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenActivationCodeIsWhitespace()
        {
            // Arrange
            _messageService.Get("CardNotFound").Returns("Card not found.");

            // Act
            var result = await _sut.ResolvePublicProfileAsync("   ");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenCardNotFound()
        {
            // Arrange
            var emptyQueryable = new List<Card>().AsQueryable().BuildMock();
            _cardRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("CardNotFound").Returns("Card not found.");

            // Act
            var result = await _sut.ResolvePublicProfileAsync("INVALID_CODE");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenCardExistsButProfileIsNull()
        {
            // Arrange
            var card = new Card
            {
                ActivationCode = "CODE123",
                IsActive = true,
                UserProfile = null  // card has no linked profile
            };
            var queryable = new List<Card> { card }.AsQueryable().BuildMock();
            _cardRepo.GetQueryable().Returns(queryable);
            _messageService.Get("CardNotFound").Returns("Card not found.");

            // Act
            var result = await _sut.ResolvePublicProfileAsync("CODE123");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsSuccess_WhenCardAndProfileExist()
        {
            // Arrange
            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FullName = "Mohamed Ahmed",
                CustomLinks =
                [
                    new ProfileLink { Id = Guid.NewGuid(), Title = "LinkedIn", Url = "https://linkedin.com/in/m" }
                ]
            };
            var card = new Card
            {
                ActivationCode = "VALID",
                IsActive = true,
                UserProfile = profile
            };

            var queryable = new List<Card> { card }.AsQueryable().BuildMock();
            _cardRepo.GetQueryable().Returns(queryable);

            var dto = new EmployeeDetailsDto
            {
                FullName = "Mohamed Ahmed",
                CustomLinks = [new ProfileLinkDto { Title = "LinkedIn" }]
            };
            _mapper.Map<EmployeeDetailsDto>(profile).Returns(dto);

            // Act
            var result = await _sut.ResolvePublicProfileAsync("VALID");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Mohamed Ahmed", result.Data!.FullName);
            Assert.Single(result.Data!.CustomLinks);
            Assert.Equal("LinkedIn", result.Data!.CustomLinks[0].Title);
        }

        // ── RecordMetricAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task RecordMetricAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            _profileRepo.GetByIdAsync(profileId).Returns((UserProfile?)null);
            _messageService.Get("RecordNotFound").Returns("Profile not found.");

            var request = new RecordMetricRequest { InteractionType = InteractionType.ProfileView };

            // Act
            var result = await _sut.RecordMetricAsync(profileId, request, null, null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task RecordMetricAsync_ReturnsSuccess_AndSavesMetricWithIpAndUserAgent()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var profile = new UserProfile { Id = profileId, TenantId = tenantId };

            _profileRepo.GetByIdAsync(profileId).Returns(profile);

            var request = new RecordMetricRequest
            {
                InteractionType = InteractionType.ContactSaved,
                ProfileLinkId = Guid.NewGuid()
            };

            // Act
            var result = await _sut.RecordMetricAsync(profileId, request, "192.168.1.1", "Mozilla/5.0");

            // Assert
            Assert.True(result.IsSuccess);
            await _metricRepo.Received(1).AddAsync(Arg.Is<ProfileMetric>(m =>
                m.UserProfileId == profileId &&
                m.TenantId == tenantId &&
                m.InteractionType == InteractionType.ContactSaved &&
                m.IpAddress == "192.168.1.1" &&
                m.UserAgent == "Mozilla/5.0" &&
                m.ProfileLinkId == request.ProfileLinkId));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task RecordMetricAsync_ReturnsSuccess_WhenIpAndUserAgentAreNull()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            var profile = new UserProfile { Id = profileId, TenantId = Guid.NewGuid() };
            _profileRepo.GetByIdAsync(profileId).Returns(profile);

            var request = new RecordMetricRequest { InteractionType = InteractionType.ProfileView };

            // Act
            var result = await _sut.RecordMetricAsync(profileId, request, null, null);

            // Assert
            Assert.True(result.IsSuccess);
            await _metricRepo.Received(1).AddAsync(Arg.Is<ProfileMetric>(m =>
                m.IpAddress == null &&
                m.UserAgent == null));
        }

        [Fact]
        public async Task RecordMetricAsync_ReturnsSuccess_WhenProfileLinkIdIsNull()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            var profile = new UserProfile { Id = profileId, TenantId = Guid.NewGuid() };
            _profileRepo.GetByIdAsync(profileId).Returns(profile);

            var request = new RecordMetricRequest
            {
                InteractionType = InteractionType.LinkClick,
                ProfileLinkId = null
            };

            // Act
            var result = await _sut.RecordMetricAsync(profileId, request, "10.0.0.1", null);

            // Assert
            Assert.True(result.IsSuccess);
            await _metricRepo.Received(1).AddAsync(Arg.Is<ProfileMetric>(m => m.ProfileLinkId == null));
        }
    }
}
