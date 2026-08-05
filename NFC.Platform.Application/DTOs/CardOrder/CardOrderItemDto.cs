using System;

namespace NFC.Platform.Application.DTOs.CardOrder;

public class CardOrderItemDto
{
    public Guid Id { get; set; }
    public Guid CardOrderId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public bool RequiresCard { get; set; } = true;
    public int NumberOfCardsRequired { get; set; } = 1;
    public Guid? UserProfileId { get; set; }
}
