using Microsoft.AspNetCore.Builder;
using NFC.Platform.BuildingBlocks.Middlewares;

namespace NFC.Platform.BuildingBlocks.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomMiddlewares(this IApplicationBuilder app)
        {
            app.UseLocalizationConfig();

            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseRateLimiter();

            return app;
        }
    }
}
