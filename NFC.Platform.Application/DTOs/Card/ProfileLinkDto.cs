using System;

namespace NFC.Platform.Application.DTOs.Card;

/// <summary>
/// Data transfer object representing a custom link on a user profile.
/// </summary>
public class ProfileLinkDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
