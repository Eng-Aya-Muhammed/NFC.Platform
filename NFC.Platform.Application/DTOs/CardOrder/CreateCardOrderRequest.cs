using System.ComponentModel.DataAnnotations;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Request payload for creating a new CardOrder.
/// Mirrors the single-page card design form in the UI.
/// </summary>
public class CreateCardOrderRequest
{
    // ── Step 1: Design Type ──────────────────────────────────────────────────
    /// <summary>
    /// Whether the customer has their own design (CustomArtwork)
    /// or needs the design team to create one (NeedCustomDesign).
    /// </summary>
    [Required]
    public CardDesignType CardDesignType { get; set; }

    // ── Step 2: Card Info ────────────────────────────────────────────────────
    [StringLength(200)]
    public string? CardName { get; set; }

    // ── Step 3: Files (Cloudinary URLs already uploaded by the frontend) ─────
    /// <summary>
    /// Cloudinary URL of the Excel file containing employee data.
    /// Only processed for CompanyAdmin users — ignored for Individual accounts.
    /// </summary>
    public string? ExcelDataUrl { get; set; }

    /// <summary>
    /// Front design file URL (PDF / PNG / AI).
    /// Required when CardDesignType = CustomArtwork.
    /// </summary>
    public string? FrontDesignUrl { get; set; }

    /// <summary>
    /// Back design file URL (PDF / PNG / AI).
    /// Required when CardDesignType = CustomArtwork.
    /// </summary>
    public string? BackDesignUrl { get; set; }

    // ── Step 4: Card Material ────────────────────────────────────────────────
    [Required]
    public CardType CardType { get; set; }

    // ── Step 5: Quantity ─────────────────────────────────────────────────────
    [Required]
    [Range(1, 10000)]
    public int Quantity { get; set; }

    // ── Step 6: Notes ────────────────────────────────────────────────────────
    public string? Notes { get; set; }
}
