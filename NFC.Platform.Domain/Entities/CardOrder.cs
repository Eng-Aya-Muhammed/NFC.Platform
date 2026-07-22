using System;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Domain.Entities
{
    public class CardOrder : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string CardName { get; set; } = string.Empty;

        public CardType CardType { get; set; }

        public CardDesignType CardDesignType { get; set; }

        /// <summary>
        /// For reorders: the parent order whose design/template is being reused.
        /// </summary>
        public Guid? ParentOrderId { get; set; }
        public CardOrder? ParentOrder { get; set; }

        public int Quantity { get; set; }

        public string? ExcelDataUrl { get; set; }

        /// <summary>
        /// Physical card design — customer upload or design-team deliverable (via TemplateRequest).
        /// This is the sole source of truth for the printed card design.
        /// </summary>
        public string? FrontDesignUrl { get; set; }

        public string? BackDesignUrl { get; set; }

        public string? Notes { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.PendingReview;

        /// <summary>
        /// Required when Status transitions to Rejected.
        /// </summary>
        public string? RejectionReason { get; set; }

        public decimal TotalPrice { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "KWD";

        public string? TrackingNumber { get; set; }

        /// <summary>
        /// 6-digit OTP sent to the recipient when order is ReadyForDelivery.
        /// Cleared after successful delivery confirmation.
        /// </summary>
        public string? DeliveryOtp { get; set; }

        /// <summary>
        /// Expiration timestamp for the delivery OTP (valid for 7 days while ReadyForDelivery).
        /// </summary>
        public DateTime? DeliveryOtpExpiresAt { get; set; }

        /// <summary>
        /// Timestamp when the last OTP notification was sent (used for 60-second cooldown rate limit).
        /// </summary>
        public DateTime? DeliveryOtpLastSentAt { get; set; }

        /// <summary>
        /// Total number of times an OTP resend was requested (maximum 5 attempts per order).
        /// </summary>
        public int DeliveryOtpResendCount { get; set; } = 0;

        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Pickup;

        /// <summary>
        /// Delivery address. Required when DeliveryMethod = Courier.
        /// Stored as free-text to match Gulf address conventions.
        /// </summary>
        public string? ShippingAddress { get; set; }

        public ICollection<CardOrderItem> Items { get; set; } = [];


    }
}
