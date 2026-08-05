using Hangfire;
using Microsoft.AspNetCore.Authorization;
using NFC.Platform.Application.DTOs.Settings;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.BuildingBlocks.Settings;
using NFC.Platform.Infrastructure.Authorization;
using NFC.Platform.Infrastructure.Contexts;
using NFC.Platform.Infrastructure.Interceptors;
using NFC.Platform.Infrastructure.Repositories;
using NFC.Platform.Infrastructure.Seeders;
using NFC.Platform.Infrastructure.Services;

namespace NFC.Platform.Infrastructure.Extensions
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<AuditableEntitySaveChangesInterceptor>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ICurrentTenant, CurrentTenantService>();
            services.AddScoped<IExcelParser, ExcelParser>();
            services.AddScoped<IExportValueFormatter, ExportValueFormatter>();
            services.AddScoped<ExportBuilder>();
            services.AddScoped<IExcelExportService, ExcelExportService>();
            services.AddScoped<IPdfExportService, PdfExportService>();

            services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
            services.AddScoped<IStorageService, CloudinaryService>();

            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
            services.AddScoped<IEmailService, EmailService>();

            services.Configure<TwilioSettings>(configuration.GetSection("TwilioSettings"));
            services.AddScoped<IWhatsAppService, WhatsAppService>();

            services.Configure<OtpSettings>(configuration.GetSection("OtpSettings"));

            services.Configure<ClientSettings>(configuration.GetSection("ClientSettings"));

            services.Configure<GoogleSettings>(configuration.GetSection("GoogleSettings"));

            services.Configure<UploadSettings>(configuration.GetSection("UploadSettings"));

            services.AddSingleton<IQrCodeService, QrCodeService>();

            services.AddSingleton<IVCardService, VCardService>();

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));
            services.AddHangfireServer();

            services.AddScoped<IRoleSeeder, RoleSeeder>();
            services.AddScoped<IPermissionSeeder, PermissionSeeder>();
            services.AddScoped<IAdminUserSeeder, AdminUserSeeder>();
            services.AddScoped<ISubscriptionPlanSeeder, SubscriptionPlanSeeder>();
            services.AddScoped<IDefaultCardTemplateSeeder, DefaultCardTemplateSeeder>();

            services.AddScoped<IPermissionCacheService, PermissionCacheService>();

            services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services;
        }
    }
}
