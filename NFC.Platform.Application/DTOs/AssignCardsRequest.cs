using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NFC.Platform.Application.DTOs
{
    /// <summary>
    /// Represents the request payload for assigning printed NFC card activation codes to order items.
    /// </summary>
    public class AssignCardsRequest
    {
        [Required]
        public List<CardAssignmentDto> Assignments { get; set; } = new();
    }

    /// <summary>
    /// Represents the link between a card order item and its physical card activation code.
    /// </summary>
    public class CardAssignmentDto
    {
        [Required]
        public Guid OrderItemId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string ActivationCode { get; set; } = string.Empty;
    }
}
