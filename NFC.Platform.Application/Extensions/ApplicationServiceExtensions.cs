using Microsoft.Extensions.DependencyInjection;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Services;

namespace NFC.Platform.Application.Extensions
{
    /// <summary>
    /// Service registration extension methods for the Application layer dependencies.
    /// </summary>
    public static class ApplicationServiceExtensions
    {
        
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICardOrderService, CardOrderService>();
            services.AddScoped<ICardPricingService, CardPricingService>();
            services.AddScoped<IEmployeeImportService, EmployeeImportService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<ICardTemplateService, CardTemplateService>();
            services.AddScoped<ITemplateRequestService, TemplateRequestService>();
            services.AddScoped<IProfileMetricService, ProfileMetricService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();

            return services;
        }
    }
}
