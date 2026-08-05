using System;

namespace NFC.Platform.Application.DTOs.Analytics;

public class EmployeeLeaderboardEntryDto
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public int TotalViews { get; set; }
    public int TotalContactSaves { get; set; }
    public int TotalLinkClicks { get; set; }
    public int TotalInteractions { get; set; }
    public int Rank { get; set; }
}
