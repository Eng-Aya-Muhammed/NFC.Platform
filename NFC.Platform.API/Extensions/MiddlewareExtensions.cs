using Microsoft.AspNetCore.Builder;
using NFC.Platform.API.Middlewares;

namespace NFC.Platform.API.Extensions
{
    /// <summary>
    /// Middleware registration extension methods for configuring the request pipeline.
    /// </summary>
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Registers custom middlewares in the application request pipeline, including request localization.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <returns>The modified application builder.</returns>
        public static IApplicationBuilder UseCustomMiddlewares(this IApplicationBuilder app)
        {
            // 1. Request Localization Middleware (must be registered first so subsequent middlewares inherit the culture)
            app.UseLocalizationConfig();

            // 2. Global Exception Middleware
            app.UseMiddleware<GlobalExceptionMiddleware>();

            return app;
        }
    }
}
