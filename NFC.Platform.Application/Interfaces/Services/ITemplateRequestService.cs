using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface ITemplateRequestService
    {
        Task<ServiceResult<TemplateRequestDto>> CreateRequestAsync(Guid userId, CreateTemplateRequest request);
        Task<ServiceResult<IReadOnlyList<TemplateRequestDto>>> GetTenantRequestsAsync();
        Task<ServiceResult<TemplateRequestDto>> GetRequestByIdAsync(Guid id);
    }
}
