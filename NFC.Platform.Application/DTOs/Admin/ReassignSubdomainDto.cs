namespace NFC.Platform.Application.DTOs.Admin;

/// <summary>
/// Request body for Super Admin subdomain reassignment.
/// Used by PUT /api/admin/subdomains/{profileId}.
/// </summary>
public class ReassignSubdomainDto
{
    public string Subdomain { get; set; } = string.Empty;
}
