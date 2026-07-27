using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services;

public interface ICardTypeService
{
    Task<ServiceResult<IReadOnlyList<CardTypeDto>>> GetActiveCardTypesAsync();
    Task<ServiceResult<PagedResult<CardTypeAdminDto>>> GetAllAdminCardTypesAsync(PaginationRequest request);
    Task<ServiceResult<CardTypeAdminDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<CardTypeAdminDto>> CreateAsync(CreateCardTypeRequest request);
    Task<ServiceResult<CardTypeAdminDto>> UpdateAsync(Guid id, UpdateCardTypeRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
