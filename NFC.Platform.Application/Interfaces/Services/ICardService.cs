using System;
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
        /// <summary>
        /// Retrieves a Card by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Card.</param>
        /// <returns>A service result wrapping the mapped CardDto if found, otherwise an error result.</returns>
        Task<ServiceResult<CardDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Retrieves a paged list of Cards based on the request settings.
        /// </summary>
        /// <param name="request">The pagination configuration request.</param>
        /// <returns>A service result wrapping a paged result of CardDtos.</returns>
        Task<ServiceResult<PagedResult<CardDto>>> GetPagedCardsAsync(PaginationRequest request);

        /// <summary>
        /// Creates and registers a new Card in the system.
        /// </summary>
        /// <param name="request">The card details payload.</param>
        /// <returns>A service result wrapping the created CardDto.</returns>
        Task<ServiceResult<CardDto>> CreateCardAsync(CreateCardRequest request);

        /// <summary>
        /// Soft deletes a Card from the system by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Card to delete.</param>
        /// <returns>A service result representing the status of the delete operation.</returns>
        Task<ServiceResult> DeleteCardAsync(Guid id);
    }
}
