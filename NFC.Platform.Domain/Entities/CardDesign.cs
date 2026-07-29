using System.ComponentModel.DataAnnotations;

namespace NFC.Platform.Domain.Entities
{
    /// <summary>
    /// Represents the card design stage — captures quantity, pricing, design files,
    /// and payment status BEFORE a CardOrder is placed.
    ///
    /// Individual accounts: select a CardPackage directly.
    /// Company accounts:    enter a CustomQuantity; pricing is calculated from
    ///                      the unit CardPackage (NumberOfCards = 1).
    /// </summary>
    public class CardDesign : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>The physical card type/material selected for this design (Wood, Plastic, Metal, etc.).</summary>
        public Guid CardTypeId { get; set; }
        public CardType? CardType { get; set; }

        /// <summary>
        /// Individual: the chosen package.
        /// Company:    the unit package (NumberOfCards = 1) used for pricing reference.
        /// </summary>
        public Guid CardPackageId { get; set; }
        public CardPackage? CardPackage { get; set; }

        /// <summary>Company-only: the total number of cards entered manually.</summary>
        public int? CustomQuantity { get; set; }

        /// <summary>
        /// Total cards available.
        /// Individual → package.NumberOfCards | Company → CustomQuantity.
        /// </summary>
        public int TotalQuantity { get; set; }

        /// <summary>
        /// Cards consumed by Approved orders so far.
        /// Deducted via Optimistic Concurrency (RowVersion) when an order is approved.
        /// </summary>
        public int UsedQuantity { get; set; } = 0;
        // RemainingQuantity = TotalQuantity - UsedQuantity (computed in AutoMapper only)

        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = "KWD";

        // ── Employees Excel (Company-only) ─────────────────────────────────────
        public string? ExcelDataUrl { get; set; }

        // ── Design Files ───────────────────────────────────────────────────────
        public CardDesignType CardDesignType { get; set; }

        /// <summary>Required when CardDesignType = CustomArtwork.</summary>
        public string? FrontDesignUrl { get; set; }

        /// <summary>Required when CardDesignType = CustomArtwork.</summary>
        public string? BackDesignUrl { get; set; }

        // ── Payment ────────────────────────────────────────────────────────────
        public bool IsPaid { get; set; } = false;
        public CardDesignPaymentStatus PaymentStatus { get; set; } = CardDesignPaymentStatus.Pending;
        public DateTime? PaidAt { get; set; }

        /// <summary>Transaction/reference ID returned by the payment gateway.</summary>
        public string? PaymentTransactionId { get; set; }

        public string? Notes { get; set; }

        // ── Optimistic Concurrency ─────────────────────────────────────────────
        /// <summary>
        /// EF row-version token. Prevents concurrent orders from double-deducting UsedQuantity.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        // ── Navigation ─────────────────────────────────────────────────────────
        public ICollection<CardOrder> Orders { get; set; } = [];
    }
}
