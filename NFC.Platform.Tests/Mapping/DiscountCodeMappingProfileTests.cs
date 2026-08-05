using System;
using AutoMapper;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class DiscountCodeMappingProfileTests
    {
        private readonly IMapper _mapper;

        public DiscountCodeMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<DiscountCodeMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void DiscountCode_To_DiscountCodeDto_MapsAllProperties()
        {
            var discountCode = new DiscountCode
            {
                Id = Guid.NewGuid(),
                Code = "SAVE50",
                DiscountValue = 50m,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            var dto = _mapper.Map<DiscountCodeDto>(discountCode);

            Assert.NotNull(dto);
            Assert.Equal("SAVE50", dto.Code);
            Assert.Equal(50m, dto.DiscountValue);
        }

        [Fact]
        public void CreateDiscountCodeRequest_To_DiscountCode_NormalizesCodeToUppercase()
        {
            var request = new CreateDiscountCodeRequest
            {
                Code = "  summer2026  ",
                DiscountValue = 25m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(14)
            };

            var entity = _mapper.Map<DiscountCode>(request);

            Assert.NotNull(entity);
            Assert.Equal("SUMMER2026", entity.Code);
            Assert.Equal(25m, entity.DiscountValue);
        }
    }
}
