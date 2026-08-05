using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services;

public interface ICardOrderService
{
    Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedOrdersAsync(PaginationRequest request, string? statusFilter, string? search = null);

    Task<ServiceResult<CardOrderDto>> GetOrderByIdAsync(Guid id);

    Task<ServiceResult<CardOrderDto>> CreateOrderAsync(CreateCardOrderRequest request);

    Task<ServiceResult<CardOrderDto>> CreateReorderAsync(Guid parentOrderId, ReorderRequest request);

    Task<ServiceResult<CardOrderDto>> UpdateOrderAsync(Guid id, UpdateCardOrderRequest request);

    Task<ServiceResult> CancelOrderAsync(Guid id);

    Task<ServiceResult> ResendOrderOtpAsync(Guid orderId);

    Task<ServiceResult<byte[]>> ExportOrdersAsync(ExportFormat format, string? statusFilter, string? search = null);
}
