using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.CardDesign;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services;

/// <summary>
/// Service contract for the CardDesign stage.
/// Handles design creation, pricing, payment URL generation, and payment callback processing.
/// </summary>
public interface ICardDesignService
{
    /// <summary>
    /// Creates a new CardDesign with pricing.
    /// Individual: uses the chosen CardPackage for pricing.
    /// Company:    uses CustomQuantity × unit-package price for pricing; optionally upserts employees from Excel.
    /// </summary>
    Task<ServiceResult<CardDesignDto>> CreateDesignAsync(CreateCardDesignRequest request);

    /// <summary>Returns a single CardDesign belonging to the current tenant.</summary>
    Task<ServiceResult<CardDesignDto>> GetDesignByIdAsync(Guid id);

    /// <summary>Returns a paged list of CardDesigns for the current tenant.</summary>
    Task<ServiceResult<PagedResult<CardDesignDto>>> GetPagedDesignsAsync(PaginationRequest request);

    /// <summary>
    /// Generates and returns the payment gateway URL for the given design.
    /// Returns an error if the design is already paid.
    /// </summary>
    Task<ServiceResult<string>> GetPaymentUrlAsync(Guid designId);

    /// <summary>
    /// Processes the payment gateway webhook callback.
    /// Verifies the HMAC signature, then marks the design as Paid or Failed.
    /// </summary>
    Task<ServiceResult> HandlePaymentCallbackAsync(Guid designId, PaymentCallbackRequest request);
}
