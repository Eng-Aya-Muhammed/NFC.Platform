using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;

namespace NFC.Platform.Application.DTOs.TemplateCategory
{
    public class TemplateCategoryExportDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_NameAr", Order = 2)]
        public string NameAr { get; set; } = string.Empty;

        [ExportColumn("Export_Col_NameEn", Order = 3)]
        public string NameEn { get; set; } = string.Empty;

        [ExportColumn("Export_Col_DisplayOrder", Order = 4)]
        public int DisplayOrder { get; set; }

        [ExportColumn("Export_Col_IsActive", Order = 5)]
        public bool IsActive { get; set; }

        [ExportColumn("Export_Col_CreatedAt", Order = 6)]
        public DateTime CreatedAt { get; set; }
    }
}
