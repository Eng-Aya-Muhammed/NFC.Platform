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
    /// <summary>
    /// Service provider extensions handling database schema migrations and data seeders.
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Automatically runs database migrations and executes seeders (RoleSeeder then AdminUserSeeder)
        /// ONLY in the Development environment.
        /// </summary>
        /// <param name="app">The application pipeline builder.</param>
        /// <param name="env">The host execution environment.</param>
        /// <returns>A task representing the database seeding process.</returns>
        public static async Task MigrateAndSeedDatabaseAsync(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (env == null) throw new ArgumentNullException(nameof(env));

            // Auto-Migration and Seeding only runs in Development mode
            if (!env.IsDevelopment())
            {
                return;
            }

            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                // 1. Run migrations automatically
                var context = services.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();

                // 2. Seed Roles
                var roleSeeder = services.GetRequiredService<IRoleSeeder>();
                await roleSeeder.SeedAsync();

                // 3. Seed Admin User
                var adminSeeder = services.GetRequiredService<IAdminUserSeeder>();
                await adminSeeder.SeedAsync();

                // 4. Seed Subscription Plans
                var planSeeder = services.GetRequiredService<ISubscriptionPlanSeeder>();
                await planSeeder.SeedAsync();
            }
            catch (Exception ex)
            {
                // Log migration failures (can be retrieved from console/file log outputs)
                var logger = services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApplicationDbContext>>();
                logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                throw;
            }
        }
    }
}
