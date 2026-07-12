using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    /// <summary>
    /// Service contract handling profile views, contact saves, and link click metrics.
    /// </summary>
    public interface IProfileMetricService
    {
        /// <summary>
        /// Records an interaction metric (view, save, link click) for a profile.
        /// </summary>
        Task<ServiceResult> RecordMetricAsync(Guid profileId, RecordMetricRequest request);

        /// <summary>
        /// Resolves and returns a public profile using the physical card's unique activation code.
        /// </summary>
        Task<ServiceResult<EmployeeDetailsDto>> ResolvePublicProfileAsync(string activationCode);
    }
}
