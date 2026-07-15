using System;

namespace NFC.Platform.Application.DTOs.Analytics;

/// <summary>
/// Single entry in the company employee analytics leaderboard.
/// Ranked by total profile interactions.
/// </summary>
public class EmployeeLeaderboardEntryDto
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public int TotalViews { get; set; }
    public int TotalContactSaves { get; set; }
    public int TotalLinkClicks { get; set; }
    public int TotalInteractions { get; set; }
    public int Rank { get; set; }
}
