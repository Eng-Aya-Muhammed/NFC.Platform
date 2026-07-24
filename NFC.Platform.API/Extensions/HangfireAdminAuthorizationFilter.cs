using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.API.Extensions
{
    public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // Allow bypass in local development environment
            var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                return true;
            }

            // In production, restrict to authenticated users in Admin role
            var user = httpContext.User;
            return user.Identity?.IsAuthenticated == true && user.IsInRole(AppRole.Admin.ToString());
        }
    }
}
