using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Analytics
{
    public class EmployeeDashboardAnalyticsDto
    {
        public int SubscriptionRemainingDays { get; set; }
        public int TotalSubscriptionDays { get; set; }

        public int MonthlyViews { get; set; }
        public int ContactSavesCount { get; set; }

        public List<MonthlyViewsTrendDto> YearlyViewsTrend { get; set; } = new();

        public double ContactSaveRate { get; set; }
        public PeakMonthDto PeakMonth { get; set; } = new();
        public int TotalYearlyViews { get; set; }
        public double AverageDailyViews { get; set; }
    }

    public class MonthlyViewsTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int ViewsCount { get; set; }
    }

    public class PeakMonthDto
    {
        public string MonthName { get; set; } = string.Empty;
        public int ViewsCount { get; set; }
        public string FormattedText { get; set; } = string.Empty;
    }

    public class TopEmployeeDto
    {
        public System.Guid EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public int TotalViews { get; set; }
        public int TotalContactSaves { get; set; }
    }
}
