using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
    /// <summary>
    /// Implementation of <see cref="ICurrentTenant"/> that resolves the active TenantId
    /// from JWT claims and validates its existence and active status in the database.
    /// </summary>
    public class CurrentTenantService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider) : ICurrentTenant
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        private bool _isTenantValidated;
        private Guid? _cachedTenantId;
        private bool _isSuperAdmin;

        private Guid? _tenantIdOverride;
        private Guid? _userIdOverride;

        /// <inheritdoc />
        public void SetCurrentTenant(Guid tenantId, Guid userId)
        {
            _tenantIdOverride = tenantId;
            _userIdOverride = userId;
            _cachedTenantId = tenantId;
            _isTenantValidated = true;
        }

        /// <inheritdoc />
        public Guid? TenantId
        {
            get
            {
                if (_tenantIdOverride.HasValue) return _tenantIdOverride;
                EnsureValidated();
                return _cachedTenantId;
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public string? Email =>
            _userIdOverride.HasValue ? "system_job@nfcplatform.com" :
            (_httpContextAccessor.HttpContext?.User?.FindFirstValue(AppClaims.Email)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email));

        /// <inheritdoc />
        public bool IsAuthenticated =>
            _userIdOverride.HasValue || (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);

        /// <inheritdoc />
        public bool IsSuperAdmin
        {
            get
            {
                EnsureValidated();
                return _isSuperAdmin;
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

            // 1. Resolve SuperAdmin status
            var roles = httpContext.User.FindAll(ClaimTypes.Role)
                .Concat(httpContext.User.FindAll(AppClaims.Role))
                .Select(c => c.Value);

            _isSuperAdmin = roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                                        || r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));

            // 2. Resolve TenantId from claim
            var tenantIdStr = httpContext.User.FindFirstValue(AppClaims.TenantId);
            if (!Guid.TryParse(tenantIdStr, out Guid tenantId))
            {
                // If it is a SuperAdmin, they may not have a tenant claim if they are a system user, or they might.
                // If they are SuperAdmin, allow null/empty TenantId bypass.
                if (_isSuperAdmin)
                {
                    _isTenantValidated = true;
                    return;
                }

                throw new ForbiddenException("Invalid or missing Tenant ID claim.");
            }

            _cachedTenantId = tenantId;

            // 3. Database validation
            // We load the tenant from database using a fresh DbContext instance to prevent
            // concurrent/nested DbContext operation conflicts during query processing.
            using (var scope = _serviceProvider.CreateScope())
            {
                var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                var interceptor = scope.ServiceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>();

                using var validationContext = new ApplicationDbContext(options, interceptor, new FakeCurrentTenant());
                // IgnoreQueryFilters is used as a best practice to ensure the query isn't intercepted.
                var tenant = validationContext.Set<Tenant>()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == tenantId)
                    .GetAwaiter().GetResult() ?? throw new ForbiddenException("The tenant associated with this account does not exist.");
                if (!tenant.IsActive)
                {
                    throw new ForbiddenException("The tenant associated with this account is currently inactive.");
                }
            }

            _isTenantValidated = true;
        }

        private sealed class FakeCurrentTenant : ICurrentTenant
        {
            public Guid? TenantId => null;
            public Guid? UserId => null;
            public string? Email => null;
            public bool IsAuthenticated => false;
            public bool IsSuperAdmin => false;
            public void SetCurrentTenant(Guid tenantId, Guid userId) { }
        }
    }
}
