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

    //  Digital profile branding 
    /// <summary>
    /// Company logo (for employee profiles) or null for individual profiles.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Layout identifier resolved from the linked CardTemplate.StyleConfigJson (e.g. "classic", "modern-dark").
    /// Frontend uses this to select which fixed layout component to render.
    /// </summary>
    public string? Layout { get; set; }

    /// <summary>
    /// Full StyleConfigJson from the resolved CardTemplate, passed through for the frontend.
    /// </summary>
    public string? StyleConfigJson { get; set; }

    /// <summary>
    /// The full public URL of this profile.
    /// Built server-side: {ClientSettings.ProfileBaseUrl}/{Subdomain}
    /// Example: "https://nfc-platform.com/u/ahmed-ali"
    /// Null if no subdomain has been assigned yet.
    /// </summary>
    public string? ProfileUrl { get; set; }
    
    /// <summary>
    /// The subdomain for the user's public profile.
    /// </summary>
    public string? Subdomain { get; set; }
}
