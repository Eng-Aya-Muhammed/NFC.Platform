using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.VipCustomer;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services;

public interface IVipCustomerService
{
    Task<ServiceResult<IReadOnlyList<VipCustomerDto>>> GetPublicVipCustomersAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<VipCustomerDto>>> GetAdminVipCustomersAsync(PaginationRequest request, CancellationToken cancellationToken = default);
}
