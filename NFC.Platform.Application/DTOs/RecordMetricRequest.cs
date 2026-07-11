using System;
using System.ComponentModel.DataAnnotations;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs;

/// <summary>
/// Request payload for logging profile interactions (clicks, views, saves).
/// </summary>
public class RecordMetricRequest
{
    [Required]
    public InteractionType InteractionType { get; set; }

    public Guid? ProfileLinkId { get; set; }
}
