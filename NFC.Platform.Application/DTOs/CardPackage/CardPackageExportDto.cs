using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;

namespace NFC.Platform.Application.DTOs.CardPackage
{
    public class CardPackageExportDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_NumberOfCards", Order = 1)]
        public int NumberOfCards { get; set; }

        [ExportColumn("Export_Col_Price", Order = 2)]
        public decimal Price { get; set; }

        [ExportColumn("Export_Col_IsActive", Order = 3)]
        public bool IsActive { get; set; }

        [ExportColumn("Export_Col_CreatedAt", Order = 4)]
        public DateTime CreatedAt { get; set; }
    }
}
