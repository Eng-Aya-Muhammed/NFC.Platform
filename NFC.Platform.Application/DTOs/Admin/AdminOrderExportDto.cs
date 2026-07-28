using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class AdminOrderExportDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_OrderNumber", Order = 2)]
        public string CardName { get; set; } = string.Empty;

        [ExportColumn("Export_Col_CompanyName", Order = 3)]
        public string CompanyName { get; set; } = string.Empty;

        [ExportColumn("Export_Col_Quantity", Order = 4)]
        public int Quantity { get; set; }

        [ExportColumn("Export_Col_TotalAmount", Order = 5)]
        public decimal TotalAmount { get; set; }

        [ExportColumn("Export_Col_Status", Order = 6)]
        public OrderStatus Status { get; set; }

        [ExportColumn("Export_Col_DeliveryMethod", Order = 7)]
        public DeliveryMethod DeliveryMethod { get; set; }

        [ExportColumn("Export_Col_CreatedAt", Order = 8)]
        public DateTime CreatedAt { get; set; }
    }
}
