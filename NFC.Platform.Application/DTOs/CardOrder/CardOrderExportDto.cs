using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder
{
    public class CardOrderExportDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_OrderNumber", Order = 2)]
        public string CardName { get; set; } = string.Empty;

        [ExportColumn("Export_Col_Quantity", Order = 3)]
        public int Quantity { get; set; }

        [ExportColumn("Export_Col_TotalAmount", Order = 4)]
        public decimal TotalAmount { get; set; }

        [ExportColumn("Export_Col_Status", Order = 5)]
        public OrderStatus Status { get; set; }

        [ExportColumn("Export_Col_DeliveryMethod", Order = 6)]
        public DeliveryMethod DeliveryMethod { get; set; }

        [ExportColumn("Export_Col_CreatedAt", Order = 7)]
        public DateTime CreatedAt { get; set; }
    }
}
