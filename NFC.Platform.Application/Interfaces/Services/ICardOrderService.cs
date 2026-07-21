using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        /// Stages the Excel file, creates an import job tracking record, and enqueues the Hangfire background job.
        /// </summary>
        Task<ServiceResult<EmployeeImportJob>> QueueEmployeeImportJobAsync(
            IFormFile file,
            CardType cardType,
            CardDesignType cardDesignType,
            string? notes);
        /// <summary>
        /// Executes the employee import background job. Runs in Hangfire thread.
        /// </summary>
        Task ProcessEmployeeImportJobAsync(Guid jobId);

        /// <summary>
        /// Retrieves the Excel ingestion status for a bulk order or import job.
        /// </summary>
        Task<ServiceResult<EmployeesImportStatusDto>> GetEmployeesImportStatusAsync(Guid id);
        Task<ServiceResult<IReadOnlyList<CardPricingDto>>> GetActivePricingCatalogAsync();

        /// <summary>
        /// Resends the delivery OTP for an order belonging to the current tenant/user.
        /// Enforces 60-second cooldown and maximum 5 resends.
        /// </summary>
        Task<ServiceResult> ResendDeliveryOtpAsync(Guid orderId);
    }
}
