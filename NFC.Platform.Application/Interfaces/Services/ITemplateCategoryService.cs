using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Interfaces.Services;

public interface ITemplateCategoryService
{
    Task<ServiceResult<IReadOnlyList<TemplateCategoryDto>>> GetActiveCategoriesAsync();
    Task<ServiceResult<PagedResult<TemplateCategoryAdminDto>>> GetAllAdminCategoriesAsync(PaginationRequest request, string? search = null);
    Task<ServiceResult<byte[]>> ExportTemplateCategoriesAsync(ExportFormat format, string? search = null);
    Task<ServiceResult<TemplateCategoryAdminDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<TemplateCategoryAdminDto>> CreateAsync(CreateTemplateCategoryRequest request);
    Task<ServiceResult<TemplateCategoryAdminDto>> UpdateAsync(Guid id, UpdateTemplateCategoryRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
