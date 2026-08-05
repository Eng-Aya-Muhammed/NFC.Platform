using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.API.Extensions
{
    public static class DatabaseExtensions
    {
        public static async Task MigrateAndSeedDatabaseAsync(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (env == null) throw new ArgumentNullException(nameof(env));

            if (!env.IsDevelopment())
            {
                return;
            }

            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();

                var roleSeeder = services.GetRequiredService<IRoleSeeder>();
                await roleSeeder.SeedAsync();

                var permissionSeeder = services.GetRequiredService<IPermissionSeeder>();
                await permissionSeeder.SeedAsync();

                var adminSeeder = services.GetRequiredService<IAdminUserSeeder>();
                await adminSeeder.SeedAsync();

                var planSeeder = services.GetRequiredService<ISubscriptionPlanSeeder>();
                await planSeeder.SeedAsync();

                var templateSeeder = services.GetRequiredService<IDefaultCardTemplateSeeder>();
                await templateSeeder.SeedAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();
                logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                throw;
            }
        }
    }
}
