using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.Role;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Entities;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.Application.Services
{
    public class RoleService(
        IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant,
        IPermissionCacheService permissionCache,
        IMessageService messageService) : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        private readonly IPermissionCacheService _permissionCache = permissionCache ?? throw new ArgumentNullException(nameof(permissionCache));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        public async Task<ServiceResult<RoleDto>> CreateRoleAsync(CreateRoleRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<RoleDto>.Unauthorized();

            var existing = await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Name == request.Name && r.TenantId == tenantId.Value);

            if (existing.Any())
                return ServiceResult<RoleDto>.Fail(_messageService.Get("RoleAlreadyExists"));

            var validPermissions = AppPermissions.GetAll().ToHashSet();
            var invalidPerms = request.Permissions.Where(p => !validPermissions.Contains(p)).ToList();
            if (invalidPerms.Count > 0)
                return ServiceResult<RoleDto>.Fail(_messageService.Get("InvalidPermissions"));

            var role = new Role
            {
                Name = request.Name,
                TenantId = tenantId.Value,
                IsSystemRole = false,
                RolePermissions = request.Permissions
                    .Select(p => new RolePermission { Permission = p })
                    .ToList()
            };

            await _unitOfWork.Repository<Role>().AddAsync(role);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<RoleDto>.Success(MapToDto(role));
        }

        public async Task<ServiceResult<IReadOnlyList<RoleDto>>> GetRolesAsync()
        {
            var tenantId = _currentTenant.TenantId;

            var roles = await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.TenantId == null || r.TenantId == tenantId);

            var roleIds = roles.Select(r => r.Id).ToList();
            var allPermissions = await _unitOfWork.Repository<RolePermission>()
                .FindAsync(rp => roleIds.Contains(rp.RoleId));

            var permissionsByRole = allPermissions
                .GroupBy(rp => rp.RoleId)
                .ToDictionary(g => g.Key, g => g.Select(rp => rp.Permission).ToList());

            IReadOnlyList<RoleDto> result = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                IsSystemRole = r.IsSystemRole,
                Permissions = permissionsByRole.TryGetValue(r.Id, out var perms)
                    ? perms
                    : []
            }).ToList();

            return ServiceResult<IReadOnlyList<RoleDto>>.Success(result);
        }

        public async Task<ServiceResult<RoleDto>> GetRoleByIdAsync(Guid roleId)
        {
            var tenantId = _currentTenant.TenantId;

            var roles = await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Id == roleId && (r.TenantId == null || r.TenantId == tenantId));

            var role = roles.Count > 0 ? roles[0] : null;
            if (role is null)
                return ServiceResult<RoleDto>.NotFound(_messageService.Get("RecordNotFound"));

            var permissions = await _unitOfWork.Repository<RolePermission>()
                .FindAsync(rp => rp.RoleId == roleId);

            role.RolePermissions = permissions.ToList();

            return ServiceResult<RoleDto>.Success(MapToDto(role));
        }

        public async Task<ServiceResult> UpdateRolePermissionsAsync(Guid roleId, AssignPermissionsRequest request)
        {
            var tenantId = _currentTenant.TenantId;

            var roles = await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Id == roleId && r.TenantId == tenantId && !r.IsSystemRole);

            if (!roles.Any())
                return ServiceResult.Fail(_messageService.Get("SystemRoleModificationNotAllowed"), 403);

            var validPermissions = AppPermissions.GetAll().ToHashSet();
            var invalidPerms = request.Permissions.Where(p => !validPermissions.Contains(p)).ToList();
            if (invalidPerms.Count > 0)
                return ServiceResult.Fail(_messageService.Get("InvalidPermissions"));

            var existingPerms = await _unitOfWork.Repository<RolePermission>()
                .FindAsync(rp => rp.RoleId == roleId);

            foreach (var perm in existingPerms)
                _unitOfWork.Repository<RolePermission>().HardRemove(perm);

            foreach (var permission in request.Permissions)
            {
                await _unitOfWork.Repository<RolePermission>().AddAsync(new RolePermission
                {
                    RoleId = roleId,
                    Permission = permission
                });
            }

            await _unitOfWork.SaveChangesAsync();

            await _permissionCache.InvalidateRoleUsersAsync(roleId);

            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult> DeleteRoleAsync(Guid roleId)
        {
            var tenantId = _currentTenant.TenantId;

            var roles = await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Id == roleId && r.TenantId == tenantId && !r.IsSystemRole);

            if (!roles.Any())
                return ServiceResult.Fail(_messageService.Get("SystemRoleModificationNotAllowed"), 403);

            await _permissionCache.InvalidateRoleUsersAsync(roleId);

            _unitOfWork.Repository<Role>().Remove(roles[0]);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RoleDeleted"));
        }

        public async Task<ServiceResult> AssignRoleToUserAsync(Guid userId, Guid roleId)
        {
            var tenantId = _currentTenant.TenantId;

            var roles = await _unitOfWork.Repository<Role>()
                .FindAsync(r => r.Id == roleId && (r.TenantId == null || r.TenantId == tenantId));

            if (!roles.Any())
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            var existing = await _unitOfWork.Repository<UserRole>()
                .FindAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existing.Any())
                return ServiceResult.Fail(_messageService.Get("UserAlreadyHasRole"));

            await _unitOfWork.Repository<UserRole>().AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });

            await _unitOfWork.SaveChangesAsync();

            _permissionCache.InvalidateUser(userId);

            return ServiceResult.Success(_messageService.Get("RoleAssigned"));
        }

        public async Task<ServiceResult> RevokeRoleFromUserAsync(Guid userId, Guid roleId)
        {
            var userRoles = await _unitOfWork.Repository<UserRole>()
                .FindAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            var userRole = userRoles.Count > 0 ? userRoles[0] : null;
            if (userRole is null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            _unitOfWork.Repository<UserRole>().HardRemove(userRole);
            await _unitOfWork.SaveChangesAsync();

            _permissionCache.InvalidateUser(userId);

            return ServiceResult.Success(_messageService.Get("RoleRevoked"));
        }

        public Task<ServiceResult<IReadOnlyList<string>>> GetAvailablePermissionsAsync()
        {
            IReadOnlyList<string> permissions = AppPermissions.GetAll().ToList();
            return Task.FromResult(ServiceResult<IReadOnlyList<string>>.Success(permissions));
        }

        private static RoleDto MapToDto(Role role) => new()
        {
            Id = role.Id,
            Name = role.Name,
            IsSystemRole = role.IsSystemRole,
            Permissions = role.RolePermissions.Select(rp => rp.Permission).ToList()
        };
    }
}
