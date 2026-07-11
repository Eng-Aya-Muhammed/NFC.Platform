using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NFC.Platform.BuildingBlocks.Extensions
{
    /// <summary>
    /// Service registration extension methods for configuring CORS policies.
    /// </summary>
    public static class CorsServiceExtensions
    {
        /// <summary>
        /// Registers a default CORS policy using origins specified dynamically in appsettings.json.
        /// </summary>
        /// <param name="services">The service collection descriptor.</param>
        /// <param name="configuration">The configuration provider to load origins.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy", policy =>
                {
                    policy.WithOrigins(origins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
}
