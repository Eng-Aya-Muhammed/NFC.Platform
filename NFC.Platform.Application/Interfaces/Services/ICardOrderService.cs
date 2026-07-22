using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Service contract handling core CardOrder business logic and data operations.
/// Focused on Single Responsibility Principle.
/// </summary>
public interface ICardOrderService
{
    /// <summary>
    /// Retrieves a paged list of CardOrders for the current tenant, optionally filtered by status.
    /// </summary>
    Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedOrdersAsync(PaginationRequest request, string? statusFilter);

    /// <summary>
    /// Retrieves a single CardOrder by its identifier, including its items.
    /// </summary>
    Task<ServiceResult<CardOrderDto>> GetOrderByIdAsync(Guid id);

    /// <summary>
    /// Creates a new CardOrder for the currently authenticated user/tenant.
    /// </summary>
    Task<ServiceResult<CardOrderDto>> CreateOrderAsync(CreateCardOrderRequest request);

    /// <summary>
    /// Creates a reorder: a new CardOrder reusing the parent order's design and template.
    /// </summary>
    Task<ServiceResult<CardOrderDto>> CreateReorderAsync(Guid parentOrderId, ReorderRequest request);

    /// <summary>
    /// Soft-deletes a CardOrder. Only allowed while Status = PendingReview.
    /// </summary>
    Task<ServiceResult> DeleteOrderAsync(Guid id);

    /// <summary>
    /// Resends the delivery OTP for an order belonging to the current tenant/user.
    /// Enforces 60-second cooldown and maximum 5 resends.
    /// </summary>
    Task<ServiceResult> ResendOrderOtpAsync(Guid orderId);
}
