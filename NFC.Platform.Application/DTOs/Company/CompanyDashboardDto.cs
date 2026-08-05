using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Company;

public class CompanyDashboardDto
{
    public int ContactSavesCount { get; set; }
    public int TotalEmployeesCount { get; set; }
    public int CardRequestsCount { get; set; }
    public string TopEmployeeName { get; set; } = string.Empty;
    public Analytics.TopEmployeeDto? TopPerformingEmployee { get; set; }
    public List<MonthlyMetricDto> MonthlyMetrics { get; set; } = [];

    public int TotalCompanyEmployees { get => TotalEmployeesCount; set => TotalEmployeesCount = value; }
    public int TotalEmployeesContactSaves { get => ContactSavesCount; set => ContactSavesCount = value; }
    public List<MonthlyMetricDto> YearlyEmployeesViewsTrend { get => MonthlyMetrics; set => MonthlyMetrics = value; }
}
