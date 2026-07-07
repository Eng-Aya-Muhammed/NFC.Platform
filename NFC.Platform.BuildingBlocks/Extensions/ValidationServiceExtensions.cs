using System;
using System.Linq;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.BuildingBlocks.Extensions
{
    /// <summary>
    /// Service registration extension methods for configuring FluentValidation validator services.
    /// </summary>
    public static class ValidationServiceExtensions
    {
        /// <summary>
        /// Registers FluentValidation services, enables automatic MVC model validation, auto-scans all assemblies for validators,
        /// and overrides the default model state validation behavior to return formatted ServiceResult errors.
        /// </summary>
        /// <param name="services">The service collection descriptor.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

            // Override ASP.NET Core automatic model state invalid response to unify validation errors
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    var result = ServiceResult.Fail(errors, 400);

                    return new BadRequestObjectResult(result);
                };
            });

            return services;
        }
    }
}
