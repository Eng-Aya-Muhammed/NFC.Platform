using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Interfaces.Services;

public interface ICardTypeService
{
    Task<ServiceResult<IReadOnlyList<CardTypeDto>>> GetActiveCardTypesAsync(string? search = null);
    Task<ServiceResult<PagedResult<CardTypeAdminDto>>> GetAllAdminCardTypesAsync(PaginationRequest request, string? search = null);
    Task<ServiceResult<byte[]>> ExportCardTypesAsync(ExportFormat format, string? search = null);
    Task<ServiceResult<CardTypeAdminDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<CardTypeAdminDto>> CreateAsync(CreateCardTypeRequest request);
    Task<ServiceResult<CardTypeAdminDto>> UpdateAsync(Guid id, UpdateCardTypeRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
