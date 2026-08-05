using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.CardDesign;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services;

public interface ICardDesignService
{
    Task<ServiceResult<CardDesignDto>> CreateDesignAsync(CreateCardDesignRequest request);

    Task<ServiceResult<CardDesignDto>> GetDesignByIdAsync(Guid id);

    Task<ServiceResult<PagedResult<CardDesignDto>>> GetPagedDesignsAsync(PaginationRequest request, string? search = null);

    Task<ServiceResult<string>> GetPaymentUrlAsync(Guid designId);

    Task<ServiceResult> HandlePaymentCallbackAsync(Guid designId, PaymentCallbackRequest request);
}
