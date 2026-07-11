using System;
using System.ComponentModel.DataAnnotations;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

    /// <summary>
    /// Request payload for updating the status of a CardOrder (admin use).
    /// </summary>
    public class UpdateCardOrderStatusRequest
    {
        [Required]
        public OrderStatus Status { get; set; }
    }

