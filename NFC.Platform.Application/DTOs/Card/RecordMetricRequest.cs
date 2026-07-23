using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Card;

/// <summary>
/// Request payload for logging profile interactions (clicks, views, saves).
/// </summary>
public class RecordMetricRequest
{
    public InteractionType InteractionType { get; set; }

    public Guid? ProfileLinkId { get; set; }
}
