using System;

namespace NFC.Platform.Application.DTOs.Admin;

public class ProfileSubdomainSummaryDto
{
    public Guid ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
}
