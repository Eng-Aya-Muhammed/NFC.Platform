using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    /// <summary>
    /// Join entity: defines which CardTemplates are accessible for a given SubscriptionPlan.
    /// Super Admin manages this mapping via the admin panel.
    /// </summary>
    public class SubscriptionPlanTemplate : BaseEntity
    {
        public Guid SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public Guid CardTemplateId { get; set; }
        public CardTemplate CardTemplate { get; set; } = null!;
    }
}
