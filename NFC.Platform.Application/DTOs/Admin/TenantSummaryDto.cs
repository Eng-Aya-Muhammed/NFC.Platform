using System;

namespace NFC.Platform.Application.DTOs.Admin
{
    public class TenantSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public string? ActivePlanName { get; set; }
        public DateTime? SubscriptionExpiry { get; set; }
        public int DaysRemaining { get; set; }
    }
}
