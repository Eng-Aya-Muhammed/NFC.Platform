using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IProfileMetricService
    {
        Task<ServiceResult> RecordMetricAsync(Guid profileId, RecordMetricRequest request);

        Task<ServiceResult<EmployeeDetailsDto>> ResolvePublicProfileAsync(Guid profileId);

        Task<ServiceResult<EmployeeDetailsDto>> ResolvePublicProfileBySubdomainAsync(string subdomain);
    }
}
