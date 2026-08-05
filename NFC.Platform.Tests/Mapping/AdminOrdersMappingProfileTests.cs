using System;
using System.Globalization;
using AutoMapper;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class AdminOrdersMappingProfileTests
    {
        private readonly IMapper _mapper;

        public AdminOrdersMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AdminMappingProfile>();
                cfg.AddProfile<CardOrderMappingProfile>();
                cfg.AddProfile<CardTypeMappingProfile>();
                cfg.AddProfile<CardPackageMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void CardOrder_To_AdminOrderSummaryDto_PopulatesCardNameAndIds()
        {
            var typeId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var order = new CardOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Tenant = new Tenant { Id = tenantId, Name = "Test Tenant" },
                Quantity = 5,
                TotalPrice = 150m,
                Status = OrderStatus.PendingReview,
                CardDesign = new CardDesign
                {
                    CardTypeId = typeId,
                    CardPackageId = packageId,
                    CardDesignType = CardDesignType.CustomArtwork,
                    CardType = new CardType { Id = typeId, NameAr = "خشب فاخر", NameEn = "Luxury Wood" },
                    CardPackage = new CardPackage { Id = packageId, NumberOfCards = 5, Price = 150m }
                }
            };

            var dto = _mapper.Map<AdminOrderSummaryDto>(order);

            Assert.NotNull(dto);
            Assert.Equal(order.Id, dto.Id);
            Assert.Equal("Test Tenant", dto.TenantName);
            Assert.Equal("خشب فاخر / Luxury Wood", dto.CardName);
            Assert.Equal(typeId, dto.CardTypeId);
            Assert.Equal(packageId, dto.CardPackageId);
            Assert.Equal(CardDesignType.CustomArtwork, dto.DesignType);
        }

        [Fact]
        public void CardOrder_To_AdminOrderDetailDto_PopulatesAllDesignUrlsAndDetails()
        {
            var typeId = Guid.NewGuid();
            var packageId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var order = new CardOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Tenant = new Tenant { Id = tenantId, Name = "Company ABC" },
                UserId = userId,
                User = new User
                {
                    Id = userId,
                    Email = "admin@abc.com",
                    UserProfile = new UserProfile { FullName = "John Doe" }
                },
                Quantity = 10,
                TotalPrice = 300m,
                UnitPrice = 30m,
                Currency = "KWD",
                Status = OrderStatus.InPrinting,
                CardDesign = new CardDesign
                {
                    CardTypeId = typeId,
                    CardPackageId = packageId,
                    CardDesignType = CardDesignType.CustomArtwork,
                    ExcelDataUrl = "https://storage.com/excel.xlsx",
                    FrontDesignUrl = "https://storage.com/front.png",
                    BackDesignUrl = "https://storage.com/back.png",
                    CardType = new CardType { Id = typeId, NameAr = "بلاستيك", NameEn = "Plastic" },
                    CardPackage = new CardPackage { Id = packageId, NumberOfCards = 10, Price = 300m }
                }
            };

            var dto = _mapper.Map<AdminOrderDetailDto>(order);

            Assert.NotNull(dto);
            Assert.Equal(order.Id, dto.Id);
            Assert.Equal("Company ABC", dto.TenantName);
            Assert.Equal("John Doe", dto.CustomerName);
            Assert.Equal("admin@abc.com", dto.CustomerEmail);
            Assert.Equal("بلاستيك / Plastic", dto.CardName);
            Assert.Equal(typeId, dto.CardTypeId);
            Assert.Equal(packageId, dto.CardPackageId);
            Assert.Equal("https://storage.com/excel.xlsx", dto.ExcelDataUrl);
            Assert.Equal("https://storage.com/front.png", dto.FrontDesignUrl);
            Assert.Equal("https://storage.com/back.png", dto.BackDesignUrl);
            Assert.Equal(CardDesignType.CustomArtwork, dto.DesignType);
            Assert.NotNull(dto.CardType);
            Assert.Equal("Plastic", dto.CardType.NameEn);
            Assert.NotNull(dto.CardPackage);
            Assert.Equal(10, dto.CardPackage.NumberOfCards);
        }

        [Fact]
        public void CardOrder_To_CardOrderDto_LocalizesCardNameBasedOnCulture()
        {
            var typeId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = Guid.NewGuid(),
                CardDesign = new CardDesign
                {
                    CardTypeId = typeId,
                    CardType = new CardType { Id = typeId, NameAr = "معدن فاخر", NameEn = "Premium Metal" }
                }
            };

            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var dtoAr = _mapper.Map<CardOrderDto>(order);

            CultureInfo.CurrentUICulture = new CultureInfo("en");
            var dtoEn = _mapper.Map<CardOrderDto>(order);

            Assert.Equal("معدن فاخر", dtoAr.CardName);
            Assert.Equal("Premium Metal", dtoEn.CardName);
        }
    }
}
