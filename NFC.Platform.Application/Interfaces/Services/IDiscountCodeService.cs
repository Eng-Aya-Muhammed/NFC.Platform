using System;
using System.Threading;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services;

public interface IDiscountCodeService
{
    Task<ServiceResult<PagedResult<DiscountCodeDto>>> GetPagedAdminAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<DiscountCodeDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<DiscountCodeDto>> CreateAsync(CreateDiscountCodeRequest request);
    Task<ServiceResult<DiscountCodeDto>> UpdateAsync(Guid id, UpdateDiscountCodeRequest request);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<DiscountCodeValidationResultDto>> ValidateCodeAsync(ValidateDiscountCodeRequest request);
}
