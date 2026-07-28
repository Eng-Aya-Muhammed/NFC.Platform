using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Employee
{
    public class EmployeeExportDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_FullName", Order = 2)]
        public string FullName { get; set; } = string.Empty;

        [ExportColumn("Export_Col_Email", Order = 3)]
        public string Email { get; set; } = string.Empty;

        [ExportColumn("Export_Col_PhoneNumber", Order = 4)]
        public string PhoneNumber { get; set; } = string.Empty;

        [ExportColumn("Export_Col_JobTitle", Order = 5)]
        public string JobTitle { get; set; } = string.Empty;

        [ExportColumn("Export_Col_Department", Order = 6)]
        public string Department { get; set; } = string.Empty;

        [ExportColumn("Export_Col_IsActive", Order = 7)]
        public bool IsActive { get; set; }

        [ExportColumn("Export_Col_CreatedAt", Order = 8)]
        public DateTime CreatedAt { get; set; }
    }
}
