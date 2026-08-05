using System;
using Microsoft.Extensions.DependencyInjection;

namespace NFC.Platform.BuildingBlocks.Extensions
{
    public static class MapperServiceExtensions
    {
        public static IServiceCollection AddAutoMapperConfig(this IServiceCollection services)
        {
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            return services;
        }
    }
}
