using System;
using System.Collections.Generic;
using AutoMapper;
using NFC.Platform.Application.DTOs.Company;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class CompanyMappingProfileTests
    {
        private readonly IMapper _mapper;

        public CompanyMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CompanyMappingProfile>();
                cfg.AddProfile<UserProfileMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Company_To_CompanyProfileDto_MapsAdminUserEmailPhoneAndCustomLinks()
        {
            // Arrange
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@techcorp.com",
                PhoneNumber = "+20111222333",
                UserProfile = new UserProfile
                {
                    CustomLinks = new List<ProfileLink>
                    {
                        new ProfileLink { Title = "Website", Url = "https://techcorp.com", DisplayOrder = 1 }
                    }
                }
            };

            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Tech Corp",
                Address = "Smart Village, Cairo",
                Activity = "Software Development",
                CommercialRegistry = "123456",
                AdminUser = adminUser
            };

            // Act
            var dto = _mapper.Map<CompanyProfileDto>(company);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("Tech Corp", dto.Name);
            Assert.Equal("admin@techcorp.com", dto.AdminUserEmail);
            Assert.Equal("+20111222333", dto.Phone);
            Assert.Single(dto.Links);
        }
    }
}
