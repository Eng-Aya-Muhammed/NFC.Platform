using System;
using System.ComponentModel.DataAnnotations;

namespace NFC.Platform.Application.DTOs.CardOrder;

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
