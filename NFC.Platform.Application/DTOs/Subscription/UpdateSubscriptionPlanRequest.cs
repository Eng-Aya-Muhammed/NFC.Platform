namespace NFC.Platform.Application.DTOs.Subscription
{
    /// <summary>Patch-semantics: only non-null fields are applied.</summary>
    public class UpdateSubscriptionPlanRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? DurationInDays { get; set; }
        public int? MaxEmployees { get; set; }

        /// <summary>SubscriptionConstants.UnlimitedQuota means unlimited. null = no change.</summary>
        public int? MaxTemplateChanges { get; set; }

        /// <summary>SubscriptionConstants.UnlimitedQuota means unlimited. null = no change.</summary>
        public int? MaxCustomDesignRequests { get; set; }

        /// <summary>Optional list of template IDs to assign to this plan. If provided, replaces existing templates.</summary>
        public List<Guid>? TemplateIds { get; set; }
    }
}
