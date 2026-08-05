using System;

namespace NFC.Platform.Application.DTOs.Card;

public class ProfileLinkDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
