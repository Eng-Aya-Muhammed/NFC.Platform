using System;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.DTOs.Profile;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IProfileService
    {
        Task<ServiceResult<EmployeeDetailsDto>> GetProfileAsync(Guid userId);
        Task<ServiceResult<EmployeeDetailsDto>> UpdateProfileAsync(Guid userId, UpdateMyProfileRequest request);
        Task<ServiceResult<EmployeeDetailsDto>> SynchronizeLinksAsync(Guid userId, SynchronizeLinksRequest request);

        /// <summary>
        /// Sets the individual user's digital profile template and optional branding overrides.
        /// Available via PATCH /api/user/profile/template.
        /// </summary>
        Task<ServiceResult<EmployeeDetailsDto>> UpdateProfileTemplateAsync(Guid userId, UpdateUserProfileTemplateRequest request);
    }
}
