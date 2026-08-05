using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Infrastructure.Contexts;
using NFC.Platform.Infrastructure.Interceptors;

namespace NFC.Platform.Infrastructure.Services
{
    public class CurrentTenantService(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        private bool _isTenantValidated;
        private Guid? _cachedTenantId;
        private bool _isAdmin;

        private Guid? _tenantIdOverride;
        private Guid? _userIdOverride;

        public void SetCurrentTenant(Guid tenantId, Guid userId)
        {
            _tenantIdOverride = tenantId;
            _userIdOverride = userId;
            _cachedTenantId = tenantId;
            _isTenantValidated = true;
        }

        public Guid? TenantId
        {
            get
            {
                if (_tenantIdOverride.HasValue) return _tenantIdOverride;
                EnsureValidated();
                return _cachedTenantId;
            }
        }

        public Guid? UserId
        {
            get
            {
                if (_userIdOverride.HasValue) return _userIdOverride;
                var user = _httpContextAccessor.HttpContext?.User;
                var userIdStr = user?.FindFirstValue(AppClaims.UserId) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(userIdStr, out Guid userId) ? userId : null;
            }
        }

        public string? Email =>
            _userIdOverride.HasValue ? "system_job@nfcplatform.com" :
            (_httpContextAccessor.HttpContext?.User?.FindFirstValue(AppClaims.Email)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email));

        public AccountType? AccountType
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var claimVal = user?.FindFirstValue(AppClaims.AccountType);
                if (!string.IsNullOrWhiteSpace(claimVal) && Enum.TryParse<AccountType>(claimVal, ignoreCase: true, out var parsed))
                {
                    return parsed;
                }
                return null;
            }
        }

        public bool IsAuthenticated =>
            _userIdOverride.HasValue || (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);

        public bool IsAdmin
        {
            get
            {
                EnsureValidated();
                return _isAdmin;
            }
        }

        private void EnsureValidated()
        {
            if (_isTenantValidated) return;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null || !IsAuthenticated)
            {
                _isTenantValidated = true;
                return;
            }

            var userRoles = httpContext.User.FindAll(ClaimTypes.Role)
                .Concat(httpContext.User.FindAll(AppClaims.Role))
                .Select(c => c.Value);

            _isAdmin = userRoles.Any(r => r.Equals(AppRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase));

            if (_isAdmin)
            {
                var adminTenantIdStr = httpContext.User.FindFirstValue(AppClaims.TenantId)
                    ?? httpContext.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");

                if (Guid.TryParse(adminTenantIdStr, out Guid adminTenantId) && adminTenantId != Guid.Empty)
                {
                    _cachedTenantId = adminTenantId;
                }

                _isTenantValidated = true;
                return;
            }

            var tenantIdStr = httpContext.User.FindFirstValue(AppClaims.TenantId)
                ?? httpContext.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");

            if (!Guid.TryParse(tenantIdStr, out Guid tenantId) || tenantId == Guid.Empty)
            {
                throw new ForbiddenException("InvalidTenantClaim");
            }

            _cachedTenantId = tenantId;
            _isTenantValidated = true;
        }
    }
}
