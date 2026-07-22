using System;
using System.Collections.Generic;
using NFC.Platform.Application.DTOs.Template;

namespace NFC.Platform.Application.DTOs
{
    public class SubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int MaxEmployees { get; set; }

        /// <summary>SubscriptionConstants.UnlimitedQuota = unlimited.</summary>
        public int MaxTemplateChanges { get; set; }

        /// <summary>SubscriptionConstants.UnlimitedQuota = unlimited.</summary>
        public int MaxCustomDesignRequests { get; set; }

        public IReadOnlyList<CardTemplateSummaryDto> AllowedTemplates { get; set; } = [];
    }
}
