using System;

namespace NFC.Platform.Application.DTOs.CardPackage;

public class CardPackageAdminDto
{
    public Guid Id { get; set; }
    public int NumberOfCards { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
