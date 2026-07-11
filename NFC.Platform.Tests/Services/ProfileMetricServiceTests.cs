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

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenCardDoesNotExistOrInactive()
        {
            // Arrange
            var emptyQueryable = new List<Card>().AsQueryable().BuildMock();
            _cardRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Profile not found.");

            // Act
            var result = await _sut.ResolvePublicProfileAsync("NONEXISTENT");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_Success_ReturnsProfileDetails()
        {
            // Arrange
            var activationCode = "ACTIVECODE123";
            var profileId = Guid.NewGuid();
            var profile = new UserProfile
            {
                Id = profileId,
                FullName = "Mohamed Ahmed",
                JobTitle = "Engineer",
                CustomLinks = new List<ProfileLink>
                {
                    new ProfileLink { Id = Guid.NewGuid(), Title = "LinkedIn", Url = "https://linkedin.com", DisplayOrder = 1 }
                }
            };
            var card = new Card { ActivationCode = activationCode, IsActive = true, UserProfile = profile };

            var queryable = new List<Card> { card }.AsQueryable().BuildMock();
            _cardRepo.GetQueryable().Returns(queryable);

            var expectedDto = new EmployeeDetailsDto
            {
                Id = profileId,
                FullName = "Mohamed Ahmed",
                CustomLinks = new List<ProfileLinkDto>
                {
                    new ProfileLinkDto { Title = "LinkedIn", Url = "https://linkedin.com", DisplayOrder = 1 }
                }
            };
            _mapper.Map<EmployeeDetailsDto>(profile).Returns(expectedDto);

            // Act
            var result = await _sut.ResolvePublicProfileAsync(activationCode);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Mohamed Ahmed", result.Data.FullName);
            Assert.Single(result.Data.CustomLinks);
            Assert.Equal("LinkedIn", result.Data.CustomLinks[0].Title);
        }

        [Fact]
        public async Task RecordMetricAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            _profileRepo.GetByIdAsync(profileId).Returns((UserProfile?)null);
            _messageService.Get("RecordNotFound").Returns("Profile not found.");

            var request = new RecordMetricRequest { InteractionType = InteractionType.ProfileView };

            // Act
            var result = await _sut.RecordMetricAsync(profileId, request, "127.0.0.1", "Chrome");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task RecordMetricAsync_Success_SavesProfileMetricRecordToDatabase()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var profile = new UserProfile { Id = profileId, TenantId = tenantId };
            _profileRepo.GetByIdAsync(profileId).Returns(profile);

            var request = new RecordMetricRequest
            {
                InteractionType = InteractionType.ContactSaved,
                ProfileLinkId = null
            };

            // Act
            var result = await _sut.RecordMetricAsync(profileId, request, "192.168.1.1", "Safari");

            // Assert
            Assert.True(result.IsSuccess);
            await _metricRepo.Received(1).AddAsync(Arg.Is<ProfileMetric>(m =>
                m.UserProfileId == profileId &&
                m.TenantId == tenantId &&
                m.InteractionType == InteractionType.ContactSaved &&
                m.IpAddress == "192.168.1.1" &&
                m.UserAgent == "Safari"
            ));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
