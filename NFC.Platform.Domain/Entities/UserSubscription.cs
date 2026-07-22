using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class UserSubscription : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        // ── Per-period usage counters (reset to 0 on each new subscription row) ─────

        /// <summary>How many times the tenant has switched their card template this period.</summary>
        public int TemplateChangesUsed { get; set; } = 0;

        /// <summary>How many custom design requests have been submitted this period.</summary>
        public int CustomDesignRequestsUsed { get; set; } = 0;
    }
}
