using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

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

                // Add Accept-Language header to all Swagger endpoints with 'ar' as default
                options.OperationFilter<AcceptLanguageHeaderOperationFilter>();

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

    /// <summary>
    /// Swagger Operation Filter to add Accept-Language header parameter to API documentation.
    /// </summary>
    public class AcceptLanguageHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Accept-Language",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Preferred language: 'ar' (Arabic) or 'en' (English)",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString("ar")
                }
            });
        }
    }
}
