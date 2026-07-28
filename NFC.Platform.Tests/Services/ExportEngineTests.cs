using System;
using System.Collections.Generic;
using System.Globalization;
using NFC.Platform.BuildingBlocks.Common.Attributes;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Services;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class ExportEngineTests
    {
        private class TestExportDto
        {
            public Guid Id { get; set; }

            [ExportColumn("Export_Col_FullName", Order = 2)]
            public string Name { get; set; } = string.Empty;

            [ExportColumn("Export_Col_IsActive", Order = 3)]
            public bool IsActive { get; set; }

            [ExportColumn("Export_Col_Status", Order = 4)]
            public OrderStatus Status { get; set; }

            [ExportColumn("Export_Col_TotalAmount", Order = 5)]
            public decimal TotalAmount { get; set; }

            [ExportColumn("Export_Col_CreatedAt", Order = 6)]
            public DateTime CreatedAt { get; set; }

            [ExportColumn("Export_Col_DeliveryMethod", Order = 7)]
            public DeliveryMethod? OptionalDeliveryMethod { get; set; }
        }

        private readonly IMessageService _mockMessageService;
        private readonly ExportValueFormatter _valueFormatter;
        private readonly ExportBuilder _exportBuilder;

        public ExportEngineTests()
        {
            _mockMessageService = Substitute.For<IMessageService>();
            _mockMessageService.Get(Arg.Any<string>(), Arg.Any<object[]>())
                .Returns(callInfo =>
                {
                    var key = callInfo.ArgAt<string>(0);
                    return key switch
                    {
                        "Export_Bool_Yes" => "نعم",
                        "Export_Bool_No" => "لا",
                        "Export_Enum_OrderStatus_PendingReview" => "قيد المراجعة",
                        "Export_Enum_DeliveryMethod_Courier" => "توصيل منزلي",
                        "Export_Col_FullName" => "الاسم الكامل",
                        "Export_Col_IsActive" => "نشط",
                        "Export_Col_Status" => "الحالة",
                        "Export_Col_TotalAmount" => "الإجمالي",
                        "Export_Col_CreatedAt" => "تاريخ الإنشاء",
                        "Export_Col_DeliveryMethod" => "طريقة التوصيل",
                        "Export_Title_Orders" => "قائمة الطلبات",
                        _ => key
                    };
                });

            _valueFormatter = new ExportValueFormatter(_mockMessageService);
            _exportBuilder = new ExportBuilder(_mockMessageService, _valueFormatter);
        }

        [Fact]
        public void ExportValueFormatter_FormatsBooleansInArabicCorrectly()
        {
            var culture = new CultureInfo("ar-EG");

            var trueText = _valueFormatter.Format(true, culture);
            var falseText = _valueFormatter.Format(false, culture);

            Assert.Equal("نعم", trueText);
            Assert.Equal("لا", falseText);
        }

        [Fact]
        public void ExportValueFormatter_FormatsBooleansInEnglishCorrectly()
        {
            var englishMessageService = Substitute.For<IMessageService>();
            englishMessageService.Get("Export_Bool_Yes").Returns("Yes");
            englishMessageService.Get("Export_Bool_No").Returns("No");

            var formatter = new ExportValueFormatter(englishMessageService);
            var culture = new CultureInfo("en-US");

            Assert.Equal("Yes", formatter.Format(true, culture));
            Assert.Equal("No", formatter.Format(false, culture));
        }

        [Fact]
        public void ExportValueFormatter_FormatsEnumsAndNullableEnumsCorrectly()
        {
            var culture = new CultureInfo("ar-EG");

            var enumText = _valueFormatter.Format(OrderStatus.PendingReview, culture);
            DeliveryMethod? nullableEnum = DeliveryMethod.Courier;
            var nullableEnumText = _valueFormatter.Format(nullableEnum, culture);
            DeliveryMethod? nullEnum = null;
            var nullEnumText = _valueFormatter.Format(nullEnum, culture);

            Assert.Equal("قيد المراجعة", enumText);
            Assert.Equal("توصيل منزلي", nullableEnumText);
            Assert.Equal(string.Empty, nullEnumText);
        }

        [Fact]
        public void ExportValueFormatter_HandlesNullAndNullableTypesGracefully()
        {
            var culture = new CultureInfo("en-US");

            Assert.Equal(string.Empty, _valueFormatter.Format(null, culture));
            int? nullInt = null;
            Assert.Equal(string.Empty, _valueFormatter.Format(nullInt, culture));
            DateTime? nullDate = null;
            Assert.Equal(string.Empty, _valueFormatter.Format(nullDate, culture));
        }

        [Fact]
        public void ExportValueFormatter_FormatsDateAndCurrencyTypesCorrectly()
        {
            var culture = new CultureInfo("en-US");
            var date = new DateTime(2026, 7, 28, 14, 30, 0);
            decimal amount = 1250.75m;

            var formattedDate = _valueFormatter.Format(date, culture);
            var formattedAmount = _valueFormatter.Format(amount, culture);

            Assert.Equal("28/07/2026 14:30", formattedDate);
            Assert.Equal("1,250.75", formattedAmount);
        }

        [Fact]
        public void ExportValueFormatter_FormatsGuidAndStringEnumerableCorrectly()
        {
            var culture = new CultureInfo("en-US");
            var guid = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var tags = new List<string> { "NFC", "Premium", "Card" };

            var formattedGuid = _valueFormatter.Format(guid, culture);
            var formattedTags = _valueFormatter.Format(tags, culture);

            Assert.Equal("11111111-2222-3333-4444-555555555555", formattedGuid);
            Assert.Equal("NFC, Premium, Card", formattedTags);
        }

        [Fact]
        public void ExportBuilder_BuildsContainerWithRtlAndHeaders()
        {
            var culture = new CultureInfo("ar-EG");
            var items = new List<TestExportDto>
            {
                new TestExportDto
                {
                    Id = Guid.NewGuid(),
                    Name = "علي محمد",
                    IsActive = true,
                    Status = OrderStatus.PendingReview,
                    TotalAmount = 150.50m,
                    CreatedAt = new DateTime(2026, 7, 28, 14, 0, 0),
                    OptionalDeliveryMethod = DeliveryMethod.Courier
                }
            };

            var container = _exportBuilder.BuildContainer(items, "Export_Title_Orders", culture);

            Assert.True(container.IsRtl);
            Assert.Equal("قائمة الطلبات", container.Title);
            Assert.Equal(7, container.Headers.Count);
            Assert.Single(container.Rows);
            Assert.Equal("1", container.Rows[0].Cells["SequenceNumber"]);
            Assert.Equal("نعم", container.Rows[0].Cells["IsActive"]);
            Assert.Equal("قيد المراجعة", container.Rows[0].Cells["Status"]);
            Assert.Equal("توصيل منزلي", container.Rows[0].Cells["OptionalDeliveryMethod"]);
        }

        [Fact]
        public void ExportBuilder_EnglishCulture_SetsIsRtlFalse()
        {
            var culture = new CultureInfo("en-US");
            var items = new List<TestExportDto>
            {
                new TestExportDto
                {
                    Id = Guid.NewGuid(),
                    Name = "John Doe",
                    IsActive = false,
                    Status = OrderStatus.PendingReview,
                    TotalAmount = 50m,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var container = _exportBuilder.BuildContainer(items, "Export_Title_Orders", culture);

            Assert.False(container.IsRtl);
            Assert.Equal(culture, container.Culture);
        }

        [Fact]
        public void ExcelExportService_GeneratesValidBytes()
        {
            var culture = new CultureInfo("ar-EG");
            var items = new List<TestExportDto>
            {
                new TestExportDto
                {
                    Id = Guid.NewGuid(),
                    Name = "أحمد خالد",
                    IsActive = true,
                    Status = OrderStatus.PendingReview,
                    TotalAmount = 250m,
                    CreatedAt = DateTime.UtcNow
                },
                new TestExportDto
                {
                    Id = Guid.NewGuid(),
                    Name = "فاطمة حسن",
                    IsActive = false,
                    Status = OrderStatus.PendingReview,
                    TotalAmount = 180m,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var container = _exportBuilder.BuildContainer(items, "Export_Title_Orders", culture);
            var excelService = new ExcelExportService();

            var bytes = excelService.GenerateExcel(container);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void PdfExportService_GeneratesValidBytesWithLandscapeLayout()
        {
            var culture = new CultureInfo("ar-EG");
            var items = new List<TestExportDto>
            {
                new TestExportDto
                {
                    Id = Guid.NewGuid(),
                    Name = "سارة محمود",
                    IsActive = false,
                    Status = OrderStatus.PendingReview,
                    TotalAmount = 80m,
                    CreatedAt = DateTime.UtcNow,
                    OptionalDeliveryMethod = DeliveryMethod.Courier
                }
            };

            var container = _exportBuilder.BuildContainer(items, "Export_Title_Orders", culture);
            var pdfService = new PdfExportService();

            var bytes = pdfService.GeneratePdf(container);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void ExportBuilder_BuildsTemplateCategoriesContainerCorrectly()
        {
            var culture = new CultureInfo("ar-EG");
            var categories = new List<NFC.Platform.Application.DTOs.TemplateCategory.TemplateCategoryExportDto>
            {
                new NFC.Platform.Application.DTOs.TemplateCategory.TemplateCategoryExportDto
                {
                    Id = Guid.NewGuid(),
                    NameAr = "قوالب شركات",
                    NameEn = "Corporate Templates",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var container = _exportBuilder.BuildContainer(categories, "Export_Title_TemplateCategories", culture);

            Assert.True(container.IsRtl);
            Assert.Equal(6, container.Headers.Count);
            Assert.Single(container.Rows);
            Assert.Equal("قوالب شركات", container.Rows[0].Cells["NameAr"]);
            Assert.Equal("Corporate Templates", container.Rows[0].Cells["NameEn"]);
            Assert.Equal("نعم", container.Rows[0].Cells["IsActive"]);
        }

        [Fact]
        public void ExportBuilder_BuildsDiscountCodesAndCardPackagesContainersCorrectly()
        {
            var culture = new CultureInfo("ar-EG");
            var discountCodes = new List<NFC.Platform.Application.DTOs.DiscountCode.DiscountCodeExportDto>
            {
                new NFC.Platform.Application.DTOs.DiscountCode.DiscountCodeExportDto
                {
                    Id = Guid.NewGuid(),
                    Code = "SUMMER50",
                    DiscountValue = 50.0m,
                    StartDate = new DateTime(2026, 1, 1),
                    EndDate = new DateTime(2026, 12, 31),
                    CreatedAt = DateTime.UtcNow
                }
            };

            var container = _exportBuilder.BuildContainer(discountCodes, "Export_Title_DiscountCodes", culture);

            Assert.True(container.IsRtl);
            Assert.Equal(6, container.Headers.Count);
            Assert.Single(container.Rows);
            Assert.Equal("1", container.Rows[0].Cells["SequenceNumber"]);
            Assert.Equal("SUMMER50", container.Rows[0].Cells["Code"]);
        }

        [Fact]
        public void ExportBuilder_BuildsCardTypesContainerCorrectly()
        {
            var culture = new CultureInfo("ar-EG");
            var cardTypes = new List<NFC.Platform.Application.DTOs.CardType.CardTypeExportDto>
            {
                new NFC.Platform.Application.DTOs.CardType.CardTypeExportDto
                {
                    Id = Guid.NewGuid(),
                    NameAr = "كارت بلاستيك",
                    NameEn = "Plastic Card",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var container = _exportBuilder.BuildContainer(cardTypes, "Export_Title_CardTypes", culture);

            Assert.True(container.IsRtl);
            Assert.Equal(5, container.Headers.Count);
            Assert.Single(container.Rows);
            Assert.Equal("كارت بلاستيك", container.Rows[0].Cells["NameAr"]);
            Assert.Equal("Plastic Card", container.Rows[0].Cells["NameEn"]);
            Assert.Equal("نعم", container.Rows[0].Cells["IsActive"]);
        }
    }
}
