using System.ComponentModel.DataAnnotations;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.DTOs.CardOrder;

public class ReissueCardRequest
{
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Pickup;

    [StringLength(500)]
    public string? ShippingAddress { get; set; }
}
