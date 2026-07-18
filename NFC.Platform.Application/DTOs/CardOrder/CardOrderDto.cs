using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

    /// <summary>
    /// Data transfer object representing a CardOrder response.
    /// </summary>
    public class CardOrderDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string CardName { get; set; } = string.Empty;
        public CardType? CardType { get; set; }
        public CardDesignType? CardDesignType { get; set; }
        public int Quantity { get; set; }
        public string? ExcelDataUrl { get; set; }
        public string? FrontDesignUrl { get; set; }
        public string? BackDesignUrl { get; set; }
        public string? DesignReferenceUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? DesignNotes { get; set; }
        public string? Notes { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DeliveryMethod DeliveryMethod { get; set; }
        public string? ShippingAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CardOrderItemDto> Items { get; set; } = [];
    }

