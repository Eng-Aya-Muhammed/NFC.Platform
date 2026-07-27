namespace NFC.Platform.Application.DTOs.CardType;

public class UpdateCardTypeRequest
{
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? PhotoUrl { get; set; }
    public bool? IsActive { get; set; }
}
