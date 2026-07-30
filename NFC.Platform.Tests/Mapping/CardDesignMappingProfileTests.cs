using System;
using System.Globalization;
using AutoMapper;
using NFC.Platform.Application.DTOs.CardDesign;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class CardDesignMappingProfileTests
    {
        private readonly IMapper _mapper;

        public CardDesignMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CardDesignMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void CardDesign_To_CardDesignDto_LocalizesCardTypeName_ArabicCulture()
        {
            // Arrange
            var cardType = new CardType { NameAr = "خشب فاخر", NameEn = "Luxury Wood" };
            var cardPackage = new CardPackage { NumberOfCards = 10 };
            var design = new CardDesign
            {
                TotalQuantity = 10,
                UsedQuantity = 3,
                CardType = cardType,
                CardPackage = cardPackage
            };

            // Act
            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var dto = _mapper.Map<CardDesignDto>(design);

            // Assert
            Assert.Equal("خشب فاخر", dto.CardTypeName);
            Assert.Equal("10 Cards Package", dto.CardPackageName);
            Assert.Equal(7, dto.RemainingQuantity);
        }

        [Fact]
        public void CardDesign_To_CardDesignDto_LocalizesCardTypeName_EnglishCulture()
        {
            // Arrange
            var cardType = new CardType { NameAr = "خشب فاخر", NameEn = "Luxury Wood" };
            var cardPackage = new CardPackage { NumberOfCards = 50 };
            var design = new CardDesign
            {
                TotalQuantity = 50,
                UsedQuantity = 20,
                CardType = cardType,
                CardPackage = cardPackage
            };

            // Act
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            var dto = _mapper.Map<CardDesignDto>(design);

            // Assert
            Assert.Equal("Luxury Wood", dto.CardTypeName);
            Assert.Equal("50 Cards Package", dto.CardPackageName);
            Assert.Equal(30, dto.RemainingQuantity);
        }
    }
}
