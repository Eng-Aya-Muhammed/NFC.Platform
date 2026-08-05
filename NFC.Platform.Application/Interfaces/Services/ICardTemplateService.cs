using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Interfaces.Services;

public interface ICardTemplateService
{
    Task<ServiceResult<IReadOnlyList<CardTemplateDto>>> GetActiveTemplatesAsync();
    Task<ServiceResult<PagedResult<CardTemplateAdminDto>>> GetAllAdminTemplatesAsync(PaginationRequest request, string? search = null);
    Task<ServiceResult<byte[]>> ExportCardTemplatesAsync(ExportFormat format, string? search = null);
    Task<ServiceResult<CardTemplateAdminDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<CardTemplateAdminDto>> CreateAsync(CreateCardTemplateRequest request);
    Task<ServiceResult<CardTemplateAdminDto>> UpdateAsync(Guid id, UpdateCardTemplateRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
