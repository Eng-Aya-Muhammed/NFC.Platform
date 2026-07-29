using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardDesign;

/// <summary>
/// Request payload for creating a new CardDesign.
///
/// Rules (enforced in CardDesignService after reading AccountType from JWT):
///   Individual  → CardPackageId required | CustomQuantity must be null
///   Company     → CustomQuantity required | CardPackageId must be null
/// </summary>
public class CreateCardDesignRequest
{
    // ── Company-only ──────────────────────────────────────────────────────
    /// <summary>Company-only: optional Excel file URL to bulk-upsert employees.</summary>
    public string? ExcelDataUrl { get; set; }

    // ── Quantity / Pricing ────────────────────────────────────────────────
    /// <summary>Individual-only: the chosen card package.</summary>
    public Guid? CardPackageId { get; set; }

    /// <summary>Company-only: total number of physical cards needed.</summary>
    public int? CustomQuantity { get; set; }

    // ── Design ────────────────────────────────────────────────────────────
    public CardDesignType CardDesignType { get; set; }

    /// <summary>Required when CardDesignType = CustomArtwork.</summary>
    public string? FrontDesignUrl { get; set; }

    /// <summary>Required when CardDesignType = CustomArtwork.</summary>
    public string? BackDesignUrl { get; set; }

    public Guid CardTypeId { get; set; }
    public string? Notes { get; set; }
}
