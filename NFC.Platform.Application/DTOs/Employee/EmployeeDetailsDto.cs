using System;

namespace NFC.Platform.Application.DTOs.Employee;

public class EmployeeDetailsDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    public List<ProfileLinkDto> Links { get; set; } = [];

    public Guid ProfileId { get; set; }

    public string? LogoUrl { get; set; }

    public string? Layout { get; set; }

    public string? StyleConfigJson { get; set; }

    public string? ProfileUrl { get; set; }

    public string? Subdomain { get; set; }
}
