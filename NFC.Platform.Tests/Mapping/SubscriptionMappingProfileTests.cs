using System;
using System.Collections.Generic;
using System.Globalization;
using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class SubscriptionMappingProfileTests
    {
        private readonly IMapper _mapper;

        public SubscriptionMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<SubscriptionMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void SubscriptionPlan_To_SubscriptionPlanDto_LocalizesNameAndMapsAllowedTemplates()
        {
            var template1 = new CardTemplate { Id = Guid.NewGuid(), NameAr = "قالب 1", NameEn = "Template 1" };
            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                NameAr = "خطة احترافية",
                NameEn = "Professional Plan",
                Price = 199.99m,
                DurationInDays = 30,
                MaxTemplateChanges = 5,
                MaxCustomDesignRequests = 2,
                PlanTemplates = new List<SubscriptionPlanTemplate>
                {
                    new SubscriptionPlanTemplate { CardTemplate = template1 }
                }
            };

            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var dtoAr = _mapper.Map<SubscriptionPlanDto>(plan);

            CultureInfo.CurrentUICulture = new CultureInfo("en");
            var dtoEn = _mapper.Map<SubscriptionPlanDto>(plan);

            Assert.Equal("خطة احترافية", dtoAr.Name);
            Assert.Single(dtoAr.AllowedTemplates);

            Assert.Equal("Professional Plan", dtoEn.Name);
            Assert.Single(dtoEn.AllowedTemplates);
        }

        [Fact]
        public void SubscriptionPlan_To_SubscriptionPlanAdminDto_MapsNameArNameEnAndTemplates()
        {
            var template = new CardTemplate { Id = Guid.NewGuid(), NameAr = "قالب أدمن", NameEn = "Admin Template" };
            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                NameAr = "خطة المؤسسات",
                NameEn = "Enterprise Plan",
                Price = 999m,
                DurationInDays = 365,
                MaxTemplateChanges = -1,
                MaxCustomDesignRequests = -1,
                PlanTemplates = new List<SubscriptionPlanTemplate>
                {
                    new SubscriptionPlanTemplate { CardTemplate = template }
                }
            };

            var dto = _mapper.Map<SubscriptionPlanAdminDto>(plan);

            Assert.NotNull(dto);
            Assert.Equal("خطة المؤسسات", dto.NameAr);
            Assert.Equal("Enterprise Plan", dto.NameEn);
            Assert.Equal(999m, dto.Price);
            Assert.Equal(365, dto.DurationInDays);
            Assert.Single(dto.AllowedTemplates);
        }

        [Fact]
        public void UserSubscription_To_UserSubscriptionDto_LocalizesPlanNameAndRemainingDays()
        {
            var plan = new SubscriptionPlan
            {
                NameAr = "الخطة البلاتينية",
                NameEn = "Platinum Plan",
                Price = 300m,
                MaxTemplateChanges = 10,
                MaxCustomDesignRequests = 3
            };

            var userSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                SubscriptionPlan = plan,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20),
                IsActive = true
            };

            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var dto = _mapper.Map<UserSubscriptionDto>(userSub);

            Assert.NotNull(dto);
            Assert.Equal("الخطة البلاتينية", dto.PlanName);
            Assert.Equal(300m, dto.Price);
            Assert.Equal(10, dto.MaxTemplateChanges);
            Assert.Equal(3, dto.MaxCustomDesignRequests);
            Assert.True(dto.RemainingDays >= 19 && dto.RemainingDays <= 20);
        }
    }
}
