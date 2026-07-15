using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    /// <summary>
    /// Service contract handling Card business logic and data retrieval operations.
    /// </summary>
    public interface ICardService
    {
        Task<ServiceResult<CardDto>> GetByIdAsync(Guid id);

        Task<ServiceResult<PagedResult<CardDto>>> GetPagedCardsAsync(PaginationRequest request);

        Task<ServiceResult<CardDto>> CreateCardAsync(CreateCardRequest request);

        Task<ServiceResult> ActivateCardAsync(ActivateCardRequest request);

        /// <summary>
        /// Marks a single card as Encoded after the NFC chip has been physically written.
        /// If all cards in the linked order are now encoded, auto-transitions order to ReadyForDelivery.
        /// </summary>
        Task<ServiceResult> MarkCardEncodedAsync(Guid cardId);

        /// <summary>
        /// Admin-explicit card activation (manual policy).
        /// </summary>
        Task<ServiceResult> ActivateCardByIdAsync(Guid cardId);

        /// <summary>
        /// Bulk-activates all cards belonging to an order (delivery time activation).
        /// </summary>
        Task<ServiceResult> ActivateAllCardsForOrderAsync(Guid orderId);

        /// <summary>
        /// Deactivates a card (lost/revoked). Public profile link stops resolving.
        /// </summary>
        Task<ServiceResult> DeactivateCardAsync(Guid cardId);

        /// <summary>
        /// Returns cards for a given order filtered by status — used by the NFC encoding tool.
        /// </summary>
        Task<ServiceResult<List<CardDto>>> GetCardsForEncodingAsync(Guid orderId, string? statusFilter);

        Task<ServiceResult> DeleteCardAsync(Guid id);
    }
}
