using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }



        /// <summary>
        /// Maximum number of template switches allowed per subscription period.
        /// SubscriptionConstants.UnlimitedQuota means unlimited.
        /// </summary>
        public int MaxTemplateChanges { get; set; } = SubscriptionConstants.UnlimitedQuota;

        /// <summary>
        /// Maximum number of custom design requests allowed per subscription period.
        /// SubscriptionConstants.UnlimitedQuota means unlimited.
        /// </summary>
        public int MaxCustomDesignRequests { get; set; } = SubscriptionConstants.UnlimitedQuota;

        //  Template Access (M2M) 
        public ICollection<SubscriptionPlanTemplate> PlanTemplates { get; set; } = [];
    }
}
