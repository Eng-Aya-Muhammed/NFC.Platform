using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        /// <summary>
        /// FK to the CardDesign that was created (and paid for) before this order.
        /// Holds the design files, quantity allocation, card type, and pricing.
        /// </summary>
        public Guid? CardDesignId { get; set; }
        public CardDesign? CardDesign { get; set; }

        /// <summary>
        /// Number of cards requested per employee (Company orders only).
        /// Individual orders use Quantity directly.
        /// </summary>
        public int QuantityPerEmployee { get; set; } = 1;

        /// <summary>
        /// For reorders: the parent order whose design is being reused.
        /// </summary>
        public Guid? ParentOrderId { get; set; }
        public CardOrder? ParentOrder { get; set; }

        public int Quantity { get; set; }

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
        /// 6-digit cryptographic hash of the OTP sent to the recipient when order is ReadyForDelivery.
        /// Cleared after successful delivery confirmation.
        /// </summary>
        public string? DeliveryOtpHash { get; set; }

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

        /// <summary>
        /// Total number of failed OTP verification attempts for delivery.
        /// Reset upon successful verification or when a new OTP is generated.
        /// </summary>
        public int DeliveryOtpFailedAttempts { get; set; } = 0;

        public ICollection<CardOrderItem> Items { get; set; } = [];

        /// <summary>
        /// Used for optimistic concurrency control to prevent race conditions during updates (e.g. OTP counters, status changes).
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
    }
}
