using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardDesign;

public class CardDesignDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    public Guid CardTypeId { get; set; }
    public string? CardTypeName { get; set; }

    public Guid CardPackageId { get; set; }
    public string? CardPackageName { get; set; }
    public int? CustomQuantity { get; set; }
    public int TotalQuantity { get; set; }
    public int UsedQuantity { get; set; }

    public int RemainingQuantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "KWD";

    public CardDesignType CardDesignType { get; set; }
    public string? FrontDesignUrl { get; set; }
    public string? BackDesignUrl { get; set; }
    public string? ExcelDataUrl { get; set; }
    public string? Notes { get; set; }

    public bool IsPaid { get; set; }
    public CardDesignPaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
