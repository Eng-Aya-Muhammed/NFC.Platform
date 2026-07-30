using System;
using System.Globalization;
using AutoMapper;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class CardTypeMappingProfileTests
    {
        private readonly IMapper _mapper;

        public CardTypeMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CardTypeMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void CardType_To_CardTypeDto_LocalizesName_ArabicCulture()
        {
            // Arrange
            var cardType = new CardType
            {
                Id = Guid.NewGuid(),
                NameAr = "خشب بامبو",
                NameEn = "Bamboo Wood",
                IsActive = true
            };

            // Act
            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var dto = _mapper.Map<CardTypeDto>(cardType);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("خشب بامبو", dto.Name);
            Assert.True(dto.IsActive);
        }

        [Fact]
        public void CardType_To_CardTypeDto_LocalizesName_EnglishCulture()
        {
            // Arrange
            var cardType = new CardType
            {
                Id = Guid.NewGuid(),
                NameAr = "خشب بامبو",
                NameEn = "Bamboo Wood",
                IsActive = true
            };

            // Act
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            var dto = _mapper.Map<CardTypeDto>(cardType);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("Bamboo Wood", dto.Name);
            Assert.True(dto.IsActive);
        }

        [Fact]
        public void CardType_To_CardTypeAdminDto_PopulatesBothNames()
        {
            // Arrange
            var cardType = new CardType
            {
                Id = Guid.NewGuid(),
                NameAr = "معدن أسود",
                NameEn = "Matte Black Metal",
                IsActive = true
            };

            // Act
            var dto = _mapper.Map<CardTypeAdminDto>(cardType);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("معدن أسود", dto.NameAr);
            Assert.Equal("Matte Black Metal", dto.NameEn);
            Assert.True(dto.IsActive);
        }
    }
}
