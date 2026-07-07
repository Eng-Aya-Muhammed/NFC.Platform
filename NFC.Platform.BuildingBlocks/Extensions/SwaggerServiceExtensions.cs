using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace NFC.Platform.BuildingBlocks.Extensions
{
    /// <summary>
    /// Service registration extension methods for configuring Swagger/OpenAPI documentation with JWT Bearer support.
    /// </summary>
    public static class SwaggerServiceExtensions
    {
        /// <summary>
        /// Registers Swagger generation options and configures Bearer token authorization in the Swagger UI.
        /// </summary>
        /// <param name="services">The service collection descriptor.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "NFC Platform API", 
                    Version = "v1",
                    Description = "NFC Card Selling Platform API backend services."
                });

                // Configure Bearer authentication in Swagger UI
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });

            return services;
        }
    }
}
