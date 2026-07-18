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

        private readonly IGenericRepository<CardTemplate> _templateRepo;
        private readonly IGenericRepository<UserProfile> _profileRepo;
        private readonly IGenericRepository<User> _userRepo;

        private readonly CardTemplateService _sut;

        public CardTemplateServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();

            _templateRepo = Substitute.For<IGenericRepository<CardTemplate>>();
            _profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _userRepo = Substitute.For<IGenericRepository<User>>();

            _unitOfWork.Repository<CardTemplate>().Returns(_templateRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_profileRepo);
            _unitOfWork.Repository<User>().Returns(_userRepo);

            _sut = new CardTemplateService(_unitOfWork, _mapper);
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

        [Fact]
        public async Task GetActiveTemplatesAsync_ExcludesInactiveTemplates()
        {
            // Arrange
            var templates = new List<CardTemplate>
            {
                new() { Id = Guid.NewGuid(), Name = "Active1", IsActive = true, DisplayOrder = 1 },
                new() { Id = Guid.NewGuid(), Name = "Inactive1", IsActive = false, DisplayOrder = 2 }
            };
            var queryable = templates.AsQueryable().BuildMock();
            _templateRepo.GetQueryable().Returns(queryable);

            var dtos = new List<CardTemplateDto>
            {
                new() { Name = "Active1" }
            };
            _mapper.Map<IReadOnlyList<CardTemplateDto>>(Arg.Any<List<CardTemplate>>()).Returns(dtos);

            // Act
            var result = await _sut.GetActiveTemplatesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
            Assert.Equal("Active1", result.Data![0].Name);
        }
    }
}
