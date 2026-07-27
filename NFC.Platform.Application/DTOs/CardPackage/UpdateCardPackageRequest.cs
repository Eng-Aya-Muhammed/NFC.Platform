namespace NFC.Platform.Application.DTOs.CardPackage;

public class UpdateCardPackageRequest
{
    public int? NumberOfCards { get; set; }
    public decimal? Price { get; set; }
    public bool? IsActive { get; set; }
}
