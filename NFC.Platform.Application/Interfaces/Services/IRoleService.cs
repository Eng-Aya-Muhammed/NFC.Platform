using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.Role;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<ServiceResult<RoleDto>> CreateRoleAsync(CreateRoleRequest request);

        Task<ServiceResult<IReadOnlyList<RoleDto>>> GetRolesAsync();

        Task<ServiceResult<RoleDto>> GetRoleByIdAsync(Guid roleId);

        Task<ServiceResult> UpdateRolePermissionsAsync(Guid roleId, AssignPermissionsRequest request);

        Task<ServiceResult> DeleteRoleAsync(Guid roleId);

        Task<ServiceResult> AssignRoleToUserAsync(Guid userId, Guid roleId);

        Task<ServiceResult> RevokeRoleFromUserAsync(Guid userId, Guid roleId);

        Task<ServiceResult<IReadOnlyList<string>>> GetAvailablePermissionsAsync();
    }
}
