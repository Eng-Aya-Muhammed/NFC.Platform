using System;
using AutoMapper;
using NFC.Platform.Application.DTOs.VipCustomer;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class VipCustomerMappingProfileTests
    {
        private readonly IMapper _mapper;

        public VipCustomerMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<VipCustomerMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Company_To_VipCustomerDto_MapsNameLogoAndCompanyType()
        {
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Global Enterprises",
                LogoUrl = "https://cdn.example.com/logo.png"
            };

            var dto = _mapper.Map<VipCustomerDto>(company);

            Assert.NotNull(dto);
            Assert.Equal("Global Enterprises", dto.Name);
            Assert.Equal("https://cdn.example.com/logo.png", dto.ImageUrl);
            Assert.Equal(VipCustomerType.Company, dto.CustomerType);
        }

        [Fact]
        public void UserProfile_To_VipCustomerDto_MapsNameProfilePictureAndIndividualType()
        {
            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                FullName = "VIP Individual",
                ProfilePictureUrl = "https://cdn.example.com/avatar.jpg"
            };

            var dto = _mapper.Map<VipCustomerDto>(profile);

            Assert.NotNull(dto);
            Assert.Equal("VIP Individual", dto.Name);
            Assert.Equal("https://cdn.example.com/avatar.jpg", dto.ImageUrl);
            Assert.Equal(VipCustomerType.Individual, dto.CustomerType);
        }
    }
}
