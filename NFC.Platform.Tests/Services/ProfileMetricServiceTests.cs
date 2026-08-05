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

            var options = Microsoft.Extensions.Options.Options.Create(new NFC.Platform.Application.DTOs.Settings.ClientSettings { ProfileBaseUrl = "http://localhost:3000/u" });
            _sut = new ProfileMetricService(_unitOfWork, _messageService, _mapper, options);
        }


        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            var emptyQueryable = new List<UserProfile>().AsQueryable().BuildMock();
            _profileRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("ProfileNotFound").Returns("Profile not found.");

            var result = await _sut.ResolvePublicProfileAsync(Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_ReturnsSuccess_WhenProfileExists()
        {
            var profileId = Guid.NewGuid();
            var profile = new UserProfile
            {
                Id = profileId,
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
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<UserProfile>()).Returns(dto);

            var result = await _sut.ResolvePublicProfileAsync(profileId);

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Mohamed Ahmed", result.Data!.FullName);
            Assert.Single(result.Data!.Links);
            Assert.Equal("LinkedIn", result.Data!.Links[0].Title);
        }


        [Fact]
        public async Task RecordMetricAsync_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            var profileId = Guid.NewGuid();
            _profileRepo.GetByIdAsync(profileId).Returns((UserProfile?)null);
            _messageService.Get("RecordNotFound").Returns("Profile not found.");

            var request = new RecordMetricRequest { InteractionType = InteractionType.ProfileView };

            var result = await _sut.RecordMetricAsync(profileId, request);

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task RecordMetricAsync_ReturnsSuccess_AndSavesMetric()
        {
            var profileId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var profile = new UserProfile { Id = profileId, TenantId = tenantId };

            _profileRepo.GetByIdAsync(profileId).Returns(profile);

            var request = new RecordMetricRequest
            {
                InteractionType = InteractionType.ContactSaved,
                ProfileLinkId = Guid.NewGuid()
            };

            var result = await _sut.RecordMetricAsync(profileId, request);

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
            var profileId = Guid.NewGuid();
            var profile = new UserProfile { Id = profileId, TenantId = Guid.NewGuid() };
            _profileRepo.GetByIdAsync(profileId).Returns(profile);

            var request = new RecordMetricRequest
            {
                InteractionType = InteractionType.LinkClick,
                ProfileLinkId = null
            };

            var result = await _sut.RecordMetricAsync(profileId, request);

            Assert.True(result.IsSuccess);
            await _metricRepo.Received(1).AddAsync(Arg.Is<ProfileMetric>(m => m.ProfileLinkId == null));
        }

        [Fact]
        public async Task ResolvePublicProfileBySubdomainAsync_ReturnsProfile_WhenSubdomainExists()
        {
            var profileId = Guid.NewGuid();
            var profile = new UserProfile
            {
                Id = profileId,
                FullName = "Ahmed Ali",
                Subdomain = "ahmed-ali"
            };
            var queryable = new List<UserProfile> { profile }.AsQueryable().BuildMock();
            _profileRepo.GetQueryable().Returns(queryable);
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<UserProfile>()).Returns(new EmployeeDetailsDto { Id = profileId, FullName = "Ahmed Ali" });

            var result = await _sut.ResolvePublicProfileBySubdomainAsync("ahmed-ali");

            Assert.True(result.IsSuccess);
            Assert.Equal("http://localhost:3000/u/ahmed-ali", result.Data!.ProfileUrl);
        }

        [Fact]
        public async Task ResolvePublicProfileBySubdomainAsync_ReturnsNotFound_WhenSubdomainDoesNotExist()
        {
            var queryable = new List<UserProfile>().AsQueryable().BuildMock();
            _profileRepo.GetQueryable().Returns(queryable);
            _messageService.Get("ProfileNotFound").Returns("Profile not found.");

            var result = await _sut.ResolvePublicProfileBySubdomainAsync("non-existent");

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }
    }
}
