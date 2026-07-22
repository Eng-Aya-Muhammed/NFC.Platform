using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Service responsible for card order pricing calculation and pricing configurations.
/// </summary>
public interface ICardPricingService
{
    /// <summary>
    /// Calculates the unit and total price for a given card type and quantity.
    /// Used to preview cost before placing an order.
    /// </summary>
    Task<ServiceResult<OrderPricingResponseDto>> CalculateOrderPricingAsync(CardType cardType, int quantity);

    /// <summary>
    /// Returns all currently active card pricing configurations.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<CardPricingDto>>> GetActiveCatalogAsync();
}
