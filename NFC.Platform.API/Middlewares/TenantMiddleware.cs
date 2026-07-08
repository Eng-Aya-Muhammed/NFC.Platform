using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NFC.Platform.BuildingBlocks.Common.Helpers;

namespace NFC.Platform.API.Middlewares
{
    /// <summary>
    /// Middleware that validates that the active tenant for an authenticated user is active and exists.
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant)
        {
            if (currentTenant.IsAuthenticated)
            {
                // Accessing the TenantId property forces validation against the database.
                // If validation fails, it throws a ForbiddenException which is caught and
                // handled by the GlobalExceptionMiddleware.
                _ = currentTenant.TenantId;
            }

            await _next(context);
        }
    }
}
