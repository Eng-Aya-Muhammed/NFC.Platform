using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

    /// <summary>
    /// Request payload for creating a new CardOrder.
    /// </summary>
    public class CreateCardOrderRequest
    {
        [StringLength(200)]
        public string? CardName { get; set; }

        public CardType? CardType { get; set; }

        public CardDesignType? CardDesignType { get; set; }

        public Guid? PrintTemplateId { get; set; }

        [Required]
        [Range(1, 10000)]
        public int Quantity { get; set; }

        public string? ExcelDataUrl { get; set; }
        public string? FrontDesignUrl { get; set; }
        public string? BackDesignUrl { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Optional list of specific employees to order cards for.
        /// If empty, the order is treated as a bulk order (quantity only).
        /// </summary>
        public List<CreateCardOrderItemRequest> Items { get; set; } = [];
    }

