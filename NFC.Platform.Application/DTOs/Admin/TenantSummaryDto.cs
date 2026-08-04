using System;
using NFC.Platform.BuildingBlocks.Common.Attributes;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class TenantSummaryDto
    {
        public Guid Id { get; set; }

        [ExportColumn("Export_Col_CompanyName", Order = 1)]
        public string Name { get; set; } = string.Empty;

        [ExportColumn("Export_Col_IsActive", Order = 2)]
        public bool IsActive { get; set; }

        [ExportColumn("Export_Col_AccountType", Order = 3)]
        public string AccountType { get; set; } = string.Empty;

        [ExportColumn("Export_Col_ActivePlanName", Order = 4)]
        public string? ActivePlanName { get; set; }

        [ExportColumn("Export_Col_SubscriptionStartDate", Order = 5)]
        public DateTime? SubscriptionStartDate { get; set; }

        [ExportColumn("Export_Col_SubscriptionExpiry", Order = 6)]
        public DateTime? SubscriptionExpiry { get; set; }

        [ExportColumn("Export_Col_DaysRemaining", Order = 7)]
        public int DaysRemaining { get; set; }
    }
}
