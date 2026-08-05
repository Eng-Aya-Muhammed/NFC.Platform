using System.Collections.Generic;
using NFC.Platform.Application.DTOs.Company;

namespace NFC.Platform.Application.DTOs.Analytics;

public class UserAnalyticsSummaryDto
{
    public int TotalProfileViews { get; set; }
    public int TotalContactSaves { get; set; }
    public int TotalLinkClicks { get; set; }

    public int SubscriptionRemainingDays { get; set; }
    public int TotalSubscriptionDays { get; set; }

    public int MonthlyViewsCount { get; set; }
    public int ContactSavesCount { get; set; }

    public List<MonthlyViewsTrendDto> YearlyViewsTrend { get; set; } = [];
    public List<MonthlyMetricDto> MonthlyViews { get; set; } = [];

    public double ContactSaveRate { get; set; }
    public PeakMonthDto PeakMonth { get; set; } = new();
    public int TotalYearlyViews { get; set; }
    public double AverageDailyViews { get; set; }
}
