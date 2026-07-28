using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;

namespace NFC.Platform.Application.DTOs.DiscountCode
{
    public class DiscountCodeExportDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_Code", Order = 1)]
        public string Code { get; set; } = string.Empty;

        [ExportColumn("Export_Col_DiscountValue", Order = 2)]
        public decimal DiscountValue { get; set; }

        [ExportColumn("Export_Col_StartDate", Order = 3)]
        public DateTime StartDate { get; set; }

        [ExportColumn("Export_Col_EndDate", Order = 4)]
        public DateTime EndDate { get; set; }

        [ExportColumn("Export_Col_CreatedAt", Order = 5)]
        public DateTime CreatedAt { get; set; }
    }
}
