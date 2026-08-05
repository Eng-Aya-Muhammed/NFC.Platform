using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Subscription
{
    public class CreateSubscriptionPlanRequest
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public List<string> Features { get; set; } = [];
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }


        public int MaxTemplateChanges { get; set; } = SubscriptionConstants.UnlimitedQuota;

        public int MaxCustomDesignRequests { get; set; } = SubscriptionConstants.UnlimitedQuota;

        public List<Guid>? TemplateIds { get; set; }
    }
}
