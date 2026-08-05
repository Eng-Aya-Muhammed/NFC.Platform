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

        public Guid? CardDesignId { get; set; }
        public CardDesign? CardDesign { get; set; }

        public int QuantityPerEmployee { get; set; } = 1;

        public Guid? ParentOrderId { get; set; }
        public CardOrder? ParentOrder { get; set; }

        public int Quantity { get; set; }

        public string? Notes { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.PendingReview;

        public string? RejectionReason { get; set; }

        public decimal TotalPrice { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "KWD";

        public string? TrackingNumber { get; set; }

        public string? DeliveryOtpHash { get; set; }

        public DateTime? DeliveryOtpExpiresAt { get; set; }

        public DateTime? DeliveryOtpLastSentAt { get; set; }

        public int DeliveryOtpResendCount { get; set; } = 0;

        public int DeliveryOtpFailedAttempts { get; set; } = 0;

        public ICollection<CardOrderItem> Items { get; set; } = [];

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
    }
}
