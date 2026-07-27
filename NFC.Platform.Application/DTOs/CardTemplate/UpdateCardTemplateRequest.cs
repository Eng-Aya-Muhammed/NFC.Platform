using System;

namespace NFC.Platform.Application.DTOs.CardTemplate;

public class UpdateCardTemplateRequest
{
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? PhotoUrl { get; set; }
    public string? FileUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}
