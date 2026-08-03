namespace NFC.Platform.Tests.Services
{
    public class ProfileBrandingTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly ProfileMetricService _sut;

        public ProfileBrandingTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _mapper = Substitute.For<IMapper>();

            _profileRepo = Substitute.For<IGenericRepository<UserProfile>>();

            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);

            // Configure Mapper to map UserProfile to EmployeeDetailsDto basic fields
            _mapper.Map<EmployeeDetailsDto>(Arg.Any<UserProfile>()).Returns(callInfo =>
            {
                var src = callInfo.Arg<UserProfile>();
                return new EmployeeDetailsDto
                {
                    Id = src.Id,
                    FullName = src.FullName,
                    JobTitle = src.JobTitle,
                    Department = src.Department ?? string.Empty
                };
            });

            var options = Microsoft.Extensions.Options.Options.Create(new NFC.Platform.Application.DTOs.Settings.ClientSettings { ProfileBaseUrl = "http://localhost:3000/u" });
            _sut = new ProfileMetricService(_unitOfWork, _messageService, _mapper, options);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_EmployeeProfile_InheritsCompanyBranding()
        {
            // Arrange
            var companyTemplate = new CardTemplate
            {
                Id = Guid.NewGuid(),
                NameAr = "Corporate Modern",
                NameEn = "Corporate Modern",
            };

            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Tech Corp",
                ProfileTemplateId = companyTemplate.Id,
                ProfileTemplate = companyTemplate
            };

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Company = company
            };

            var profileId = Guid.NewGuid();
            var profile = new UserProfile
            {
                Id = profileId,
                FullName = "Alice Smith",
                JobTitle = "Senior Engineer",
                Department = "Engineering",
                Employee = employee
            };

            var profileQueryable = new List<UserProfile> { profile }.AsQueryable().BuildMock();
            _profileRepo.GetQueryable().Returns(profileQueryable);

            // Act
            var result = await _sut.ResolvePublicProfileAsync(profileId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Alice Smith", result.Data.FullName);
            Assert.Null(result.Data.LogoUrl);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_IndividualProfile_UsesOwnBranding()
        {
            // Arrange
            var personalTemplate = new CardTemplate
            {
                Id = Guid.NewGuid(),
                NameAr = "Minimalist Light",
                NameEn = "Minimalist Light",
            };

            var profileId = Guid.NewGuid();
            var profile = new UserProfile
            {
                Id = profileId,
                FullName = "John Doe",
                JobTitle = "Freelancer",
                Department = "",
                Employee = null, // Individual account
                ProfileTemplateId = personalTemplate.Id,
                ProfileTemplate = personalTemplate
            };

            var profileQueryable = new List<UserProfile> { profile }.AsQueryable().BuildMock();
            _profileRepo.GetQueryable().Returns(profileQueryable);

            // Act
            var result = await _sut.ResolvePublicProfileAsync(profileId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("John Doe", result.Data.FullName);
            Assert.Null(result.Data.LogoUrl); // Individuals have no company logo
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_NoTemplateSelected_UsesSystemDefaults()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            var profile = new UserProfile
            {
                Id = profileId,
                FullName = "Bob Vance",
                JobTitle = "Manager",
                Employee = null,
                ProfileTemplateId = null,
                ProfileTemplate = null
            };

            var profileQueryable = new List<UserProfile> { profile }.AsQueryable().BuildMock();
            _profileRepo.GetQueryable().Returns(profileQueryable);

            // Act
            var result = await _sut.ResolvePublicProfileAsync(profileId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Bob Vance", result.Data.FullName);
            Assert.Null(result.Data.LogoUrl);
            Assert.Null(result.Data.Layout); // Fallback removed
            Assert.Null(result.Data.StyleConfigJson);
        }
    }
}
