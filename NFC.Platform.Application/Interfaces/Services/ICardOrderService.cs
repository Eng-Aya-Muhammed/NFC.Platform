using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    /// <summary>
    /// Service contract handling CardOrder business logic and data operations.
    /// </summary>
    public interface ICardOrderService
    {
        /// <summary>
        /// Retrieves a paged list of CardOrders for the current tenant, optionally filtered by status.
        /// </summary>
        Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedAsync(PaginationRequest request, string? statusFilter);

        /// <summary>
        /// Retrieves a single CardOrder by its identifier, including its items.
        /// </summary>
        Task<ServiceResult<CardOrderDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Calculates unit price and total price for a given card material and quantity.
        /// </summary>
        Task<ServiceResult<OrderPricingResponseDto>> GetOrderPricingAsync(string cardType, int quantity);

        /// <summary>
        /// Creates a new CardOrder for the currently authenticated user/tenant.
        /// </summary>
        Task<ServiceResult<CardOrderDto>> CreateAsync(CreateCardOrderRequest request);

        /// <summary>
        /// Creates a reorder: a new CardOrder reusing the parent order's design and template.
        /// </summary>
        Task<ServiceResult<CardOrderDto>> CreateReorderAsync(Guid parentOrderId, ReorderRequest request);


        /// <summary>
        /// Soft-deletes a CardOrder. Only allowed while Status = PendingReview.
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);


        /// <summary>
        /// Handles bulk card ordering by importing employees from an Excel stream and placing their card requests.
        /// </summary>
        Task<ServiceResult<CardOrderDto>> ImportEmployeesAndCreateBulkOrderAsync(CreateBulkCardOrderFromExcelRequest request);

        /// <summary>
        /// Retrieves the Excel ingestion status for a bulk order.
        /// </summary>
        Task<ServiceResult<EmployeesImportStatusDto>> GetEmployeesImportStatusAsync(Guid orderId);
    }
}
