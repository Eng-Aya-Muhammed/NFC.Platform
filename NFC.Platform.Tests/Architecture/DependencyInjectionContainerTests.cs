using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using NFC.Platform.API.Controllers;
using NFC.Platform.API.Extensions;
using NFC.Platform.API.Services;
using NFC.Platform.Application.Extensions;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Extensions;
using NFC.Platform.Infrastructure.Contexts;
using NFC.Platform.Infrastructure.Extensions;

namespace NFC.Platform.Tests.Architecture
{
    public class DependencyInjectionContainerTests
    {
        [Fact]
        public void ServiceProvider_ShouldResolve_AllControllersAndDependencies_WithoutErrors()
        {
            // 1. Build test configuration with all required appsettings sections
            var configurationValues = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=NfcPlatformDb;Trusted_Connection=True;TrustServerCertificate=True;" },
                { "JwtSettings:Key", "SUPER_SECRET_KEY_FOR_TESTING_PURPOSES_THAT_IS_LONG_ENOUGH_12345!" },
                { "JwtSettings:Secret", "SUPER_SECRET_KEY_FOR_TESTING_PURPOSES_THAT_IS_LONG_ENOUGH_12345!" },
                { "JwtSettings:Issuer", "NfcPlatformTestIssuer" },
                { "JwtSettings:Audience", "NfcPlatformTestAudience" },
                { "JwtSettings:ExpiryMinutes", "60" },
                { "CloudinarySettings:CloudName", "test-cloud-name" },
                { "CloudinarySettings:ApiKey", "123456789012345" },
                { "CloudinarySettings:ApiSecret", "test-api-secret-key-12345" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

            // 2. Setup ServiceCollection mimicking Program.cs
            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

            // Register DbContext using UseSqlServer
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddCorsPolicy(configuration);
            services.AddJwtAuthentication(configuration);
            services.AddAutoMapperConfig();
            services.AddFluentValidationConfig();
            services.AddLocalizationConfig();
            services.AddDistributedMemoryCache();
            services.AddInfrastructureServices(configuration);
            services.AddApplicationServices();

            // Register all controllers from the API assembly
            var apiAssembly = typeof(AuthController).Assembly;
            var controllerTypes = apiAssembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            foreach (var type in controllerTypes)
            {
                services.AddTransient(type);
            }

            // 3. Build ServiceProvider (with scope validation enabled to catch lifetime mismatches)
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            // 4. Assert that EVERY controller in the API assembly can be successfully instantiated via DI
            var resolutionFailures = new List<string>();

            using (var scope = serviceProvider.CreateScope())
            {
                foreach (var controllerType in controllerTypes)
                {
                    try
                    {
                        var controller = scope.ServiceProvider.GetService(controllerType);
                        if (controller == null)
                        {
                            controller = ActivatorUtilities.CreateInstance(scope.ServiceProvider, controllerType);
                        }

                        Assert.NotNull(controller);
                    }
                    catch (Exception ex)
                    {
                        resolutionFailures.Add($"Failed to resolve {controllerType.Name}: {ex.Message}");
                    }
                }
            }

            // 5. Assert zero resolution failures across all controllers
            Assert.Empty(resolutionFailures);
        }
    }
}
