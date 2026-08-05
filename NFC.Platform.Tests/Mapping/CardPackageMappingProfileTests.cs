using System;
using AutoMapper;
using NFC.Platform.Application.DTOs.CardPackage;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class CardPackageMappingProfileTests
    {
        private readonly IMapper _mapper;

        public CardPackageMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CardPackageMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void CardPackage_To_CardPackageDto_MapsNumberOfCardsAndPrice()
        {
            var package = new CardPackage
            {
                Id = Guid.NewGuid(),
                NumberOfCards = 25,
                Price = 250m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var dto = _mapper.Map<CardPackageDto>(package);

            Assert.NotNull(dto);
            Assert.Equal(package.Id, dto.Id);
            Assert.Equal(25, dto.NumberOfCards);
            Assert.Equal(250m, dto.Price);
            Assert.True(dto.IsActive);
        }

        [Fact]
        public void CardPackage_To_CardPackageAdminDto_MapsAllProperties()
        {
            var package = new CardPackage
            {
                Id = Guid.NewGuid(),
                NumberOfCards = 100,
                Price = 900m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var dto = _mapper.Map<CardPackageAdminDto>(package);

            Assert.NotNull(dto);
            Assert.Equal(100, dto.NumberOfCards);
            Assert.Equal(900m, dto.Price);
            Assert.True(dto.IsActive);
        }
    }
}
