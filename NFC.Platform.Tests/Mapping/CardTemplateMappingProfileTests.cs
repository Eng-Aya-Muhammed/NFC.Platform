using System;
using System.Globalization;
using AutoMapper;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class CardTemplateMappingProfileTests
    {
        private readonly IMapper _mapper;

        public CardTemplateMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CardTemplateMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void CardTemplate_To_CardTemplateDto_PopulatesCategoryName_Localized()
        {
            var category = new TemplateCategory { NameAr = "تصاميم شخصية", NameEn = "Personal Designs" };
            var template = new CardTemplate
            {
                Id = Guid.NewGuid(),
                NameAr = "قالب ذهبي",
                NameEn = "Gold Template",
                Category = category
            };

            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var dtoAr = _mapper.Map<CardTemplateDto>(template);

            CultureInfo.CurrentUICulture = new CultureInfo("en");
            var dtoEn = _mapper.Map<CardTemplateDto>(template);

            Assert.Equal("قالب ذهبي", dtoAr.Name);
            Assert.Equal("تصاميم شخصية", dtoAr.CategoryName);

            Assert.Equal("Gold Template", dtoEn.Name);
            Assert.Equal("Personal Designs", dtoEn.CategoryName);
        }

        [Fact]
        public void CardTemplate_To_CardTemplateAdminDto_PopulatesCategoryNameArAndEn()
        {
            var category = new TemplateCategory { NameAr = "تصاميم شركات", NameEn = "Corporate Designs" };
            var template = new CardTemplate
            {
                Id = Guid.NewGuid(),
                NameAr = "قالب الأعمال",
                NameEn = "Business Template",
                Category = category
            };

            var dto = _mapper.Map<CardTemplateAdminDto>(template);

            Assert.Equal("قالب الأعمال", dto.NameAr);
            Assert.Equal("Business Template", dto.NameEn);
            Assert.Equal("تصاميم شركات", dto.CategoryNameAr);
            Assert.Equal("Corporate Designs", dto.CategoryNameEn);
        }
    }
}
