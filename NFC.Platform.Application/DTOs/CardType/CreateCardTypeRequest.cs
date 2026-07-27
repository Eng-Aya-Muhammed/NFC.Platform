namespace NFC.Platform.Application.DTOs.CardType;

public class CreateCardTypeRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
