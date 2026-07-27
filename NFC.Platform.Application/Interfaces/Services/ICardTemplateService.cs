using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services;

public interface ICardTemplateService
{
    Task<ServiceResult<IReadOnlyList<CardTemplateDto>>> GetActiveTemplatesAsync();
    Task<ServiceResult<PagedResult<CardTemplateAdminDto>>> GetAllAdminTemplatesAsync(PaginationRequest request);
    Task<ServiceResult<CardTemplateAdminDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<CardTemplateAdminDto>> CreateAsync(CreateCardTemplateRequest request);
    Task<ServiceResult<CardTemplateAdminDto>> UpdateAsync(Guid id, UpdateCardTemplateRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
