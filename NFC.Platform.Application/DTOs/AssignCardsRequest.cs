using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NFC.Platform.Application.DTOs;

/// <summary>
/// Represents the request payload for assigning printed NFC card activation codes to order items.
/// </summary>
public class AssignCardsRequest
{
    [Required]
    public List<CardAssignmentDto> Assignments { get; set; } = new();
}
