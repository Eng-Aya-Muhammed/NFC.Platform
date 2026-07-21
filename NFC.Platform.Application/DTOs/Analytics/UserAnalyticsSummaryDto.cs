using System.Collections.Generic;
using NFC.Platform.Application.DTOs.Company;

namespace NFC.Platform.Application.DTOs.Analytics;

/// <summary>
/// Aggregated analytics summary for an individual user's profile.
/// Returned by GET /api/analytics/summary.
/// </summary>
public class UserAnalyticsSummaryDto
{
    public int TotalProfileViews { get; set; }
    public int TotalContactSaves { get; set; }
    public int TotalLinkClicks { get; set; }

    public List<MonthlyMetricDto> MonthlyViews { get; set; } = [];
}
