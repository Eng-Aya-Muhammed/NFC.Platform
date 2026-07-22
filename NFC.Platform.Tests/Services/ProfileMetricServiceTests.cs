namespace NFC.Platform.Tests.Services
{
    public class ProfileMetricServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;


        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<ProfileMetric> _metricRepo;

        private readonly ProfileMetricService _sut;

        public ProfileMetricServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _mapper = Substitute.For<IMapper>();

            _mapper.Map<ProfileMetric>(Arg.Any<RecordMetricRequest>()).Returns(x =>
            {
                var req = (RecordMetricRequest)x[0];
                return new ProfileMetric
                {
                    InteractionType = req.InteractionType,
                    ProfileLinkId = req.ProfileLinkId
                };
            });


            _profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _metricRepo = Substitute.For<IGenericRepository<ProfileMetric>>();


            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);
            _unitOfWork.Repository<ProfileMetric>().Returns(_metricRepo);

            _sut = new ProfileMetricService(_unitOfWork, _messageService, _mapper);
        }

        // ── ResolvePublicProfileAsync ─────────────────────────────────────────────

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenSubdomainIsNull()
        {
            // Arrange
            _messageService.Get("ProfileNotFound").Returns("Profile not found.");

            // Act
            var result = await _sut.ResolvePublicProfileAsync(null!);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenSubdomainIsWhitespace()
        {
            // Arrange
            _messageService.Get("ProfileNotFound").Returns("Profile not found.");

            // Act
            var result = await _sut.ResolvePublicProfileAsync("   ");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenSubdomainDoesNotExist()
        {
            // Arrange
            var emptyQueryable = new List<UserProfile>().AsQueryable().BuildMock();
            _profileRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("ProfileNotFound").Returns("Profile not found.");

            // Act
            var result = await _sut.ResolvePublicProfileAsync("unknown-subdomain");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsSuccess_WhenSubdomainExists()
        {
            // Arrange
            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                Subdomain = "ghaith",
                FullName = "Mohamed Ahmed",
                CustomLinks =
                [
                    new ProfileLink { Id = Guid.NewGuid(), Title = "LinkedIn", Url = "https://linkedin.com/in/m" }
                ]
            };

            var queryable = new List<UserProfile> { profile }.AsQueryable().BuildMock();
            _profileRepo.GetQueryable().Returns(queryable);

            var dto = new EmployeeDetailsDto
            {
                FullName = "Mohamed Ahmed",
                Links = [new ProfileLinkDto { Title = "LinkedIn" }]
            };
            _mapper.Map<EmployeeDetailsDto>(profile).Returns(dto);

            // Act
            var result = await _sut.ResolvePublicProfileAsync("ghaith");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Mohamed Ahmed", result.Data!.FullName);
            Assert.Single(result.Data!.Links);
            Assert.Equal("LinkedIn", result.Data!.Links[0].Title);
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
            var result = await _sut.RecordMetricAsync(profileId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task RecordMetricAsync_ReturnsSuccess_AndSavesMetric()
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
            var result = await _sut.RecordMetricAsync(profileId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _metricRepo.Received(1).AddAsync(Arg.Is<ProfileMetric>(m =>
                m.UserProfileId == profileId &&
                m.TenantId == tenantId &&
                m.InteractionType == InteractionType.ContactSaved &&
                m.ProfileLinkId == request.ProfileLinkId));
            await _unitOfWork.Received(1).SaveChangesAsync();
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
            var result = await _sut.RecordMetricAsync(profileId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _metricRepo.Received(1).AddAsync(Arg.Is<ProfileMetric>(m => m.ProfileLinkId == null));
        }


    }
}
