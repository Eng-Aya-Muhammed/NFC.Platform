using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.CardPackage;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Interfaces.Services;

public interface ICardPackageService
{
    Task<ServiceResult<IReadOnlyList<CardPackageDto>>> GetActiveCardPackagesAsync();
    Task<ServiceResult<PagedResult<CardPackageAdminDto>>> GetAllAdminCardPackagesAsync(PaginationRequest request);
    Task<ServiceResult<byte[]>> ExportCardPackagesAsync(ExportFormat format);
    Task<ServiceResult<CardPackageAdminDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<CardPackageAdminDto>> CreateAsync(CreateCardPackageRequest request);
    Task<ServiceResult<CardPackageAdminDto>> UpdateAsync(Guid id, UpdateCardPackageRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
