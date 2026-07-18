using System;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Domain.Entities
{
    public class Card : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public string UniqueCode { get; set; } = string.Empty;

        /// <summary>
        /// The full public profile URL, e.g. https://onpoint-teasting.com/c/{UniqueCode}.
        /// </summary>
        public string ProfileUrl { get; set; } = string.Empty;

        public CardStatus Status { get; set; } = CardStatus.PendingGeneration;

        public DateTime? ActivatedAt { get; set; }

        public Guid? UserProfileId { get; set; }
        public UserProfile? UserProfile { get; set; }

        public Guid? CardOrderId { get; set; }
        public CardOrder? CardOrder { get; set; }

        // Computed helper — kept for backwards-compatible query convenience
        public bool IsActive => Status == CardStatus.Active;

        // Legacy column kept for backward compatibility; use UniqueCode going forward
        public string ActivationCode
        {
            get => UniqueCode;
            set => UniqueCode = value;
        }

        /// <summary>
        /// Cloudinary URL of the QR code image that encodes this card's ProfileUrl.
        /// Populated atomically during the InPrinting card-generation step.
        /// Null until that step runs (e.g. PendingReview orders).
        /// </summary>
        public string? QrCodeUrl { get; set; }
    }
}
