using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

public class UpdateCardOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
