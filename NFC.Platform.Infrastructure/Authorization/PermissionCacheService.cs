using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Authorization
{
    public class PermissionCacheService(
        IMemoryCache cache,
        IUnitOfWork unitOfWork) : IPermissionCacheService
    {
        private readonly IMemoryCache _cache = cache;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public static string CacheKey(Guid userId) => $"permissions_{userId}";

        /// <inheritdoc/>
        public void InvalidateUser(Guid userId)
        {
            _cache.Remove(CacheKey(userId));
        }

        /// <inheritdoc/>
        public async Task InvalidateRoleUsersAsync(Guid roleId)
        {
            var userRoles = await _unitOfWork.Repository<UserRole>()
                .FindAsync(ur => ur.RoleId == roleId);

            foreach (var ur in userRoles)
            {
                _cache.Remove(CacheKey(ur.UserId));
            }
        }
    }
}
