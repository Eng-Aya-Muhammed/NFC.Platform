using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class AdminOrderExportDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_CompanyName", Order = 2)]
        public string CompanyName { get; set; } = string.Empty;

        [ExportColumn("Export_Col_Quantity", Order = 3)]
        public int Quantity { get; set; }

        [ExportColumn("Export_Col_TotalAmount", Order = 4)]
        public decimal TotalAmount { get; set; }

        [ExportColumn("Export_Col_Status", Order = 5)]
        public OrderStatus Status { get; set; }

        [ExportColumn("Export_Col_CreatedAt", Order = 6)]
        public DateTime CreatedAt { get; set; }
    }
}
