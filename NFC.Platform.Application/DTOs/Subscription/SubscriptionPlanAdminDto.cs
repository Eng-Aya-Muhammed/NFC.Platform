using System;
using System.Collections.Generic;
using NFC.Platform.Application.DTOs.Template;

namespace NFC.Platform.Application.DTOs.Subscription;

public class SubscriptionPlanAdminDto
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationInDays { get; set; }
    public int MaxTemplateChanges { get; set; }
    public int MaxCustomDesignRequests { get; set; }

    public IReadOnlyList<CardTemplateSummaryDto> AllowedTemplates { get; set; } = [];
}
