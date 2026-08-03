namespace NFC.Platform.Application.DTOs.Settings;

/// <summary>
/// Strongly-typed options for client-facing (frontend) URL configuration.
/// Bound from the "ClientSettings" section in appsettings.json.
/// </summary>
public class ClientSettings
{
    /// <summary>
    /// Base URL for the password-reset page.
    /// Example: "https://nfc-platform.com/reset-password"
    /// </summary>
    public string ResetPasswordUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for public user profile pages.
    /// The subdomain slug is appended at runtime: {ProfileBaseUrl}/{subdomain}
    /// Example: "https://nfc-platform.com/u"  →  "https://nfc-platform.com/u/ahmed-ali"
    /// </summary>
    public string ProfileBaseUrl { get; set; } = string.Empty;
}
