using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class SubscriptionPlanTemplate : BaseEntity
    {
        public Guid SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public Guid CardTemplateId { get; set; }
        public CardTemplate CardTemplate { get; set; } = null!;
    }
}
