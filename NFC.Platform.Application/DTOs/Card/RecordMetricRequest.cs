using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.Card;

public class RecordMetricRequest
{
    public InteractionType InteractionType { get; set; }

    public Guid? ProfileLinkId { get; set; }
}
