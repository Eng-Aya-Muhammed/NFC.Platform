using System;
using System.Linq;
using System.Security.Claims;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.API.Extensions
{
    public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext == null) return false;

            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var roles = user.FindAll(ClaimTypes.Role)
                .Concat(user.FindAll(AppClaims.Role))
                .Select(c => c.Value);

            return roles.Any(r => r.Equals(AppRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
