using System;

namespace NFC.Platform.Application.DTOs
{
    public class UserSubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RemainingDays { get; set; }
        public bool IsActive { get; set; }

        // ── Plan Limits (copied for frontend convenience) ─────────────────────
        public int MaxTemplateChanges { get; set; }
        public int MaxCustomDesignRequests { get; set; }

        // ── Per-period usage ──────────────────────────────────────────────────
        public int TemplateChangesUsed { get; set; }
        public int CustomDesignRequestsUsed { get; set; }
    }
}
