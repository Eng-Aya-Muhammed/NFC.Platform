using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class CardTemplate : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? FileUrl { get; set; }
        public Guid CategoryId { get; set; }
        public TemplateCategory Category { get; set; } = null!;
        public ICollection<SubscriptionPlanTemplate> PlanTemplates { get; set; } = [];
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}
