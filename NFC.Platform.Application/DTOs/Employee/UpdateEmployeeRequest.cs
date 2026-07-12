using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Employee;

public class UpdateEmployeeRequest
{
    public string? FullName { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public UserStatus Status { get; set; }

    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }

    public string? LinkedInUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? WebsiteUrl { get; set; }

    public string? CustomLinks { get; set; }
}

