using System.ComponentModel.DataAnnotations;

namespace NFC.Platform.Domain.Entities
{
    public class CardDesign : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid CardTypeId { get; set; }
        public CardType? CardType { get; set; }

        public Guid CardPackageId { get; set; }
        public CardPackage? CardPackage { get; set; }

        public int? CustomQuantity { get; set; }

        public int TotalQuantity { get; set; }

        public int UsedQuantity { get; set; } = 0;

        public int PendingQuantity { get; set; } = 0;

        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = "KWD";

        public string? ExcelDataUrl { get; set; }

        public CardDesignType CardDesignType { get; set; }

        public string? FrontDesignUrl { get; set; }

        public string? BackDesignUrl { get; set; }

        public bool IsPaid { get; set; } = false;
        public CardDesignPaymentStatus PaymentStatus { get; set; } = CardDesignPaymentStatus.Pending;
        public DateTime? PaidAt { get; set; }

        public string? PaymentTransactionId { get; set; }

        public string? Notes { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        public ICollection<CardOrder> Orders { get; set; } = [];
    }
}
