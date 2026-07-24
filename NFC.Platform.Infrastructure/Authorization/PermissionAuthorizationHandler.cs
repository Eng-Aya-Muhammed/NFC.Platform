using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Infrastructure.Authorization
{
    public class PermissionAuthorizationHandler(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache)
        : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IMemoryCache _cache = cache;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var userIdStr = context.User.FindFirst(AppClaims.UserId)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return;

            // Admin overrides all permissions (God Mode)
            if (context.User.HasClaim(c => c.Type == AppClaims.Role && c.Value == AppRole.Admin.ToString()))
            {
                context.Succeed(requirement);
                return;
            }

            var cacheKey = PermissionCacheService.CacheKey(userId);

            if (!_cache.TryGetValue(cacheKey, out IReadOnlySet<string>? userPermissions))
            {
                userPermissions = await LoadPermissionsFromDbAsync(userId);
                _cache.Set(cacheKey, userPermissions, CacheExpiry);
            }

            if (userPermissions!.Contains(requirement.Permission))
                context.Succeed(requirement);
        }

        private async Task<IReadOnlySet<string>> LoadPermissionsFromDbAsync(Guid userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var userRoles = await uow.Repository<UserRole>()
                .FindAsync(ur => ur.UserId == userId);

            var roleIds = userRoles.Select(ur => ur.RoleId).ToHashSet();
            if (roleIds.Count == 0)
                return new HashSet<string>();

            var rolePermissions = await uow.Repository<RolePermission>()
                .FindAsync(rp => roleIds.Contains(rp.RoleId));

            return rolePermissions
                .Select(rp => rp.Permission)
                .ToHashSet();
        }
    }
}
