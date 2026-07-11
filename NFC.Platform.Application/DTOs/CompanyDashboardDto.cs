using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs;

/// <summary>
/// Data transfer object containing the dashboard statistics for a company.
/// </summary>
public class CompanyDashboardDto
{
    public int ContactSavesCount { get; set; }
    public int TotalEmployeesCount { get; set; }
    public int CardRequestsCount { get; set; }
    public string TopEmployeeName { get; set; } = string.Empty;
    public List<MonthlyMetricDto> MonthlyMetrics { get; set; } = new();
}
