using System;

namespace NFC.Platform.Application.DTOs.Admin;

/// <summary>
/// Lightweight summary of a UserProfile's subdomain for Super Admin oversight.
/// Returned by GET /api/admin/subdomains.
/// </summary>
public class ProfileSubdomainSummaryDto
{
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
}
