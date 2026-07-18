using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class ProfileBrandingTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly IGenericRepository<Card> _cardRepo;
        private readonly IGenericRepository<TemplateRequest> _templateRequestRepo;
        private readonly ProfileMetricService _sut;

        public ProfileBrandingTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _mapper = Substitute.For<IMapper>();

            _cardRepo = Substitute.For<IGenericRepository<Card>>();
            _templateRequestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();

            _unitOfWork.Repository<Card>().Returns(_cardRepo);
            _unitOfWork.Repository<TemplateRequest>().Returns(_templateRequestRepo);

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

            _sut = new ProfileMetricService(_unitOfWork, _messageService, _mapper);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_EmployeeProfile_InheritsCompanyBranding()
        {
            // Arrange
            var companyTemplate = new CardTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Corporate Modern",
                StyleConfigJson = "{\"layout\":\"modern-dark\",\"primaryColor\":\"#FF5733\",\"secondaryColor\":\"#00FF00\"}"
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

            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FullName = "Alice Smith",
                JobTitle = "Senior Engineer",
                Department = "Engineering",
                Employee = employee
            };

            var card = new Card
            {
                Id = Guid.NewGuid(),
                UniqueCode = "EMP_CODE",
                Status = CardStatus.Active,
                UserProfileId = profile.Id,
                UserProfile = profile
            };

            // Set up template request mock containing the company logo
            var templateRequest = new TemplateRequest
            {
                TenantId = profile.TenantId,
                Status = TemplateRequestStatus.Completed,
                LogoUrl = "https://cdn.example.com/techcorp-logo.png",
                CreatedAt = DateTime.UtcNow
            };

            var cardQueryable = new List<Card> { card }.AsQueryable().BuildMock();
            _cardRepo.GetQueryable().Returns(cardQueryable);

            var requestQueryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(requestQueryable);

            // Act
            var result = await _sut.ResolvePublicProfileAsync("EMP_CODE");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Alice Smith", result.Data.FullName);
            Assert.Equal("https://cdn.example.com/techcorp-logo.png", result.Data.LogoUrl);
            Assert.Equal("modern-dark", result.Data.Layout);
            Assert.Equal(companyTemplate.StyleConfigJson, result.Data.StyleConfigJson);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_IndividualProfile_UsesOwnBranding()
        {
            // Arrange
            var personalTemplate = new CardTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Minimalist Light",
                StyleConfigJson = "{\"layout\":\"minimalist\",\"primaryColor\":\"#4A90E2\",\"secondaryColor\":\"#FFFFFF\"}"
            };

            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                JobTitle = "Freelancer",
                Department = "",
                Employee = null, // Individual account
                ProfileTemplateId = personalTemplate.Id,
                ProfileTemplate = personalTemplate
            };

            var card = new Card
            {
                Id = Guid.NewGuid(),
                UniqueCode = "IND_CODE",
                Status = CardStatus.Active,
                UserProfileId = profile.Id,
                UserProfile = profile
            };

            var cardQueryable = new List<Card> { card }.AsQueryable().BuildMock();
            _cardRepo.GetQueryable().Returns(cardQueryable);

            var requestQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(requestQueryable);

            // Act
            var result = await _sut.ResolvePublicProfileAsync("IND_CODE");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("John Doe", result.Data.FullName);
            Assert.Null(result.Data.LogoUrl); // Individuals have no company logo
            Assert.Equal("minimalist", result.Data.Layout);
            Assert.Equal(personalTemplate.StyleConfigJson, result.Data.StyleConfigJson);
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_NoTemplateSelected_UsesSystemDefaults()
        {
            // Arrange
            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FullName = "Bob Vance",
                JobTitle = "Manager",
                Employee = null,
                ProfileTemplateId = null,
                ProfileTemplate = null
            };

            var card = new Card
            {
                Id = Guid.NewGuid(),
                UniqueCode = "DEFAULT_CODE",
                Status = CardStatus.Active,
                UserProfileId = profile.Id,
                UserProfile = profile
            };

            var cardQueryable = new List<Card> { card }.AsQueryable().BuildMock();
            _cardRepo.GetQueryable().Returns(cardQueryable);

            var requestQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(requestQueryable);

            // Act
            var result = await _sut.ResolvePublicProfileAsync("DEFAULT_CODE");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Bob Vance", result.Data.FullName);
            Assert.Null(result.Data.LogoUrl);
            Assert.Equal("classic", result.Data.Layout); // Fallback layout
            Assert.Null(result.Data.StyleConfigJson);
        }
    }
}
