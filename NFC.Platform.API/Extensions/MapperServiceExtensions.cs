using System;
using Microsoft.Extensions.DependencyInjection;

namespace NFC.Platform.API.Extensions
{
    /// <summary>
    /// Service registration extension methods for configuring AutoMapper mapping services.
    /// </summary>
    public static class MapperServiceExtensions
    {
        /// <summary>
        /// Registers AutoMapper configurations scanning all assemblies in the current AppDomain.
        /// </summary>
        /// <param name="services">The service collection descriptor.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddAutoMapperConfig(this IServiceCollection services)
        {
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            return services;
        }
    }
}
