using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs.CardDesign;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CardDesignServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IEmployeeService _employeeService;
        private readonly IConfiguration _configuration;
        private readonly IGenericRepository<CardDesign> _designRepo;
        private readonly CardDesignService _sut;

        public CardDesignServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _employeeService = Substitute.For<IEmployeeService>();
            _configuration = Substitute.For<IConfiguration>();
            _designRepo = Substitute.For<IGenericRepository<CardDesign>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CardDesignMappingProfile>();
            });
            _mapper = config.CreateMapper();

            _unitOfWork.Repository<CardDesign>().Returns(_designRepo);

            _sut = new CardDesignService(_unitOfWork, _mapper, _messageService, _currentTenant, _employeeService, _configuration);
        }

        [Fact]
        public async Task GetDesignByIdAsync_ReturnsNotFound_WhenDesignDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _designRepo.GetQueryable().Returns(new List<CardDesign>().AsQueryable().BuildMock());
            _messageService.Get("DesignNotFound").Returns("Design not found.");

            // Act
            var result = await _sut.GetDesignByIdAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetDesignByIdAsync_ReturnsSuccess_WhenDesignExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var design = new CardDesign
            {
                Id = id,
                TotalQuantity = 100,
                UsedQuantity = 25,
                CardType = new CardType { NameAr = "بلاستيك", NameEn = "Plastic" },
                CardPackage = new CardPackage { NumberOfCards = 100 }
            };
            _designRepo.GetQueryable().Returns(new List<CardDesign> { design }.AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetDesignByIdAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Data!.Id);
            Assert.Equal(75, result.Data.RemainingQuantity);
            Assert.Equal("100 Cards Package", result.Data.CardPackageName);
        }

        [Fact]
        public async Task GetPagedDesignsAsync_ReturnsPagedDesigns_WithCardPackageIncluded()
        {
            // Arrange
            var designs = new List<CardDesign>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TotalQuantity = 20,
                    UsedQuantity = 5,
                    CreatedAt = DateTime.UtcNow,
                    CardType = new CardType { NameAr = "خشب", NameEn = "Wood" },
                    CardPackage = new CardPackage { NumberOfCards = 20 }
                }
            };
            _designRepo.GetQueryable().Returns(designs.AsQueryable().BuildMock());

            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _sut.GetPagedDesignsAsync(pagination);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!.Items);
            Assert.Equal("20 Cards Package", result.Data.Items.First().CardPackageName);
            Assert.Equal(15, result.Data.Items.First().RemainingQuantity);
        }

        [Fact]
        public async Task GetPagedDesignsAsync_FiltersBySearch_MatchesCardTypeName()
        {
            // Arrange
            var d1 = new CardDesign { Id = Guid.NewGuid(), CardType = new CardType { NameAr = "خشب", NameEn = "Wood" } };
            var d2 = new CardDesign { Id = Guid.NewGuid(), CardType = new CardType { NameAr = "بلاستيك", NameEn = "Plastic" } };

            _designRepo.GetQueryable().Returns(new List<CardDesign> { d1, d2 }.AsQueryable().BuildMock());

            var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _sut.GetPagedDesignsAsync(pagination, "Wood");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data!.TotalCount);
        }
    }
}
