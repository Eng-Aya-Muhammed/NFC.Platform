using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

/// <summary>
/// Request payload for updating the status of a CardOrder (admin use).
/// </summary>
public class UpdateCardOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
