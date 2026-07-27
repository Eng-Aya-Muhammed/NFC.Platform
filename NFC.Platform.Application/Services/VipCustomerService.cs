using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.VipCustomer;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Services;

public class VipCustomerService(IUnitOfWork unitOfWork, IMapper mapper) : IVipCustomerService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<ServiceResult<IReadOnlyList<VipCustomerDto>>> GetPublicVipCustomersAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _unitOfWork.Repository<Company>()
            .GetQueryable()
            .AsNoTracking()
            .Where(c => c.IsVip && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var profiles = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .Where(p => p.IsVip && p.EmployeeId == null && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var combinedList = _mapper.Map<List<VipCustomerDto>>(companies)
            .Concat(_mapper.Map<List<VipCustomerDto>>(profiles))
            .OrderBy(x => x.VipDisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return ServiceResult<IReadOnlyList<VipCustomerDto>>.Success(combinedList);
    }

    public async Task<ServiceResult<PagedResult<VipCustomerDto>>> GetAdminVipCustomersAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new PaginationRequest();

        var companies = await _unitOfWork.Repository<Company>()
            .GetQueryable()
            .AsNoTracking()
            .Where(c => c.IsVip && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var profiles = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .Where(p => p.IsVip && p.EmployeeId == null && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var combinedList = _mapper.Map<List<VipCustomerDto>>(companies)
            .Concat(_mapper.Map<List<VipCustomerDto>>(profiles))
            .OrderBy(x => x.VipDisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var pagedResult = await combinedList.ToPagedResultAsync(request, cancellationToken);
        return ServiceResult<PagedResult<VipCustomerDto>>.Success(pagedResult);
    }
}
