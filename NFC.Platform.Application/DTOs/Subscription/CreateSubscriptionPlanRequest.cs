using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Subscription
{
    public class CreateSubscriptionPlanRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int MaxEmployees { get; set; }

        /// <summary>SubscriptionConstants.UnlimitedQuota means unlimited.</summary>
        public int MaxTemplateChanges { get; set; } = SubscriptionConstants.UnlimitedQuota;

        /// <summary>SubscriptionConstants.UnlimitedQuota means unlimited.</summary>
        public int MaxCustomDesignRequests { get; set; } = SubscriptionConstants.UnlimitedQuota;

        /// <summary>Optional initial set of template IDs to assign to this plan.</summary>
        public List<Guid>? TemplateIds { get; set; }
    }
}
