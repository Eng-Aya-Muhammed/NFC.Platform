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

        public Guid? PrintTemplateId { get; set; }
        public CardTemplate? PrintTemplate { get; set; }

        public int Quantity { get; set; }

        public string? ExcelDataUrl { get; set; }

        public string? FrontDesignUrl { get; set; }

        public string? BackDesignUrl { get; set; }

        public string? Notes { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal TotalPrice { get; set; }

        public ICollection<CardOrderItem> Items { get; set; } = [];

        public ICollection<Card> GeneratedCards { get; set; } = [];
    }
}
