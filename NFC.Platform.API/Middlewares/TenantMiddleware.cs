using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.API.Middlewares
{
    public class TenantMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

        public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant, ApplicationDbContext dbContext)
        {
            if (currentTenant.IsAuthenticated && !currentTenant.IsAdmin)
            {
                var tenantId = currentTenant.TenantId;
                if (tenantId.HasValue)
                {
                    var tenant = await dbContext.Set<Tenant>()
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == tenantId.Value && !t.IsDeleted);

                    if (tenant == null)
                    {
                        throw new ForbiddenException("TenantNotFound");
                    }

                    if (!tenant.IsActive)
                    {
                        throw new ForbiddenException("TenantInactive");
                    }
                }
            }

            await _next(context);
        }
    }
}
