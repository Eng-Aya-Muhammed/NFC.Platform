using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Analytics
{
    public class CompanyDashboardAnalyticsDto
    {
        public int TotalEmployees { get; set; }
        public int TotalContactSaves { get; set; }
        public EmployeeLeaderboardEntryDto? MostVisitedEmployee { get; set; }
        public List<MonthlyViewsTrendDto> TimeSeriesData { get; set; } = [];
    }
}
