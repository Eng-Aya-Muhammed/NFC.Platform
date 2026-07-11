using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.Infrastructure.Contexts;
using NFC.Platform.Infrastructure.Interceptors;
using NFC.Platform.Infrastructure.Repositories;
using NFC.Platform.Infrastructure.Seeders;
using NFC.Platform.Infrastructure.Services;

namespace NFC.Platform.Infrastructure.Extensions
{
    /// <summary>
    /// Service registration extension methods for the Infrastructure layer dependencies.
    /// </summary>
    public static class InfrastructureServiceExtensions
    {
        /// <summary>
        /// Registers DbContext, Generic Repository, Unit of Work, Token Service, and Seeders in the service collection.
        /// </summary>
        /// <param name="services">The service collection descriptor.</param>
        /// <param name="configuration">The configuration provider to load ConnectionStrings.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Register Auditable Interceptor
            services.AddScoped<AuditableEntitySaveChangesInterceptor>();

            // 2. Register DbContext (SQL Server)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 3. Register Generic Repository and Unit of Work
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 4. Register Token Service, Current Tenant Service, and Storage Service
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ICurrentTenant, CurrentTenantService>();
            
            // Cloudinary Registration
            services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
            services.AddScoped<IStorageService, CloudinaryService>();

            // 5. Register Seeders
            services.AddScoped<IRoleSeeder, RoleSeeder>();
            services.AddScoped<IAdminUserSeeder, AdminUserSeeder>();

            return services;
        }
    }
}
