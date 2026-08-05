using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public List<string> Features { get; set; } = [];
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }



        public int MaxTemplateChanges { get; set; } = SubscriptionConstants.UnlimitedQuota;

        public int MaxCustomDesignRequests { get; set; } = SubscriptionConstants.UnlimitedQuota;

        public ICollection<SubscriptionPlanTemplate> PlanTemplates { get; set; } = [];
    }
}
