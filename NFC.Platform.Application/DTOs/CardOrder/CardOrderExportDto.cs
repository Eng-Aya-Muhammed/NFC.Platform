using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder
{
    public class CardOrderExportDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_Quantity", Order = 2)]
        public int Quantity { get; set; }

        [ExportColumn("Export_Col_TotalAmount", Order = 3)]
        public decimal TotalAmount { get; set; }

        [ExportColumn("Export_Col_Status", Order = 4)]
        public OrderStatus Status { get; set; }

        [ExportColumn("Export_Col_CreatedAt", Order = 5)]
        public DateTime CreatedAt { get; set; }
    }
}
