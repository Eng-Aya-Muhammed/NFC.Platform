using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    /// <summary>
    /// Service contract handling CardOrder business logic and data operations.
    /// </summary>
    public interface ICardOrderService
    {
        /// <summary>
        /// Retrieves a paged list of CardOrders for the current tenant.
        /// </summary>
        Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedAsync(PaginationRequest request);

        /// <summary>
        /// Retrieves a single CardOrder by its identifier, including its items.
        /// </summary>
        Task<ServiceResult<CardOrderDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Creates a new CardOrder for the currently authenticated user/tenant.
        /// </summary>
        Task<ServiceResult<CardOrderDto>> CreateAsync(CreateCardOrderRequest request);

        /// <summary>
        /// Updates the status of a CardOrder (admin operation).
        /// </summary>
        Task<ServiceResult> UpdateStatusAsync(Guid id, UpdateCardOrderStatusRequest request);

        /// <summary>
        /// Soft-deletes a CardOrder by its identifier.
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// Assigns printed card activation codes to order items, automatically creating and activating cards.
        /// </summary>
        Task<ServiceResult> AssignCardsAsync(Guid orderId, AssignCardsRequest request);
    }
}
