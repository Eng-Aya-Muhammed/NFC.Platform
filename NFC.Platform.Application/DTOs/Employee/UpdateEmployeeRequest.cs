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

    public System.Collections.Generic.List<Profile.CustomLinkInput> Links { get; set; } = [];

    public string? Subdomain { get; set; }
}

