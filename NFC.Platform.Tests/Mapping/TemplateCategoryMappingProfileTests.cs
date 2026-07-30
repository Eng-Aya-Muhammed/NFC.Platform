using System;
using System.Globalization;
using AutoMapper;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class TemplateCategoryMappingProfileTests
    {
        private readonly IMapper _mapper;

        public TemplateCategoryMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TemplateCategoryMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void TemplateCategory_To_TemplateCategoryDto_LocalizesName_ArabicCulture()
        {
            // Arrange
            var category = new TemplateCategory
            {
                Id = Guid.NewGuid(),
                NameAr = "تصاميم فندقية",
                NameEn = "Hospitality Designs",
                DisplayOrder = 2,
                IsActive = true
            };

            // Act
            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var dto = _mapper.Map<TemplateCategoryDto>(category);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("تصاميم فندقية", dto.Name);
            Assert.Equal(2, dto.DisplayOrder);
            Assert.True(dto.IsActive);
        }

        [Fact]
        public void TemplateCategory_To_TemplateCategoryDto_LocalizesName_EnglishCulture()
        {
            // Arrange
            var category = new TemplateCategory
            {
                Id = Guid.NewGuid(),
                NameAr = "تصاميم فندقية",
                NameEn = "Hospitality Designs",
                DisplayOrder = 2,
                IsActive = true
            };

            // Act
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            var dto = _mapper.Map<TemplateCategoryDto>(category);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("Hospitality Designs", dto.Name);
            Assert.Equal(2, dto.DisplayOrder);
            Assert.True(dto.IsActive);
        }

        [Fact]
        public void TemplateCategory_To_TemplateCategoryAdminDto_PopulatesBothNames()
        {
            // Arrange
            var category = new TemplateCategory
            {
                Id = Guid.NewGuid(),
                NameAr = "تصاميم طبية",
                NameEn = "Medical Designs",
                DisplayOrder = 1,
                IsActive = true
            };

            // Act
            var dto = _mapper.Map<TemplateCategoryAdminDto>(category);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("تصاميم طبية", dto.NameAr);
            Assert.Equal("Medical Designs", dto.NameEn);
            Assert.Equal(1, dto.DisplayOrder);
            Assert.True(dto.IsActive);
        }
    }
}
