namespace NFC.Platform.Application.DTOs;

/// <summary>
/// Represents the total interaction count grouped by month.
/// </summary>
public class MonthlyMetricDto
{
    public string MonthName { get; set; } = string.Empty;
    public int Value { get; set; }
}
