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
            services.AddHttpClient();

            services.AddScoped<ICardOrderService, CardOrderService>();
            services.AddScoped<ICardDesignService, CardDesignService>();
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
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IDiscountCodeService, DiscountCodeService>();
            services.AddScoped<IVipCustomerService, VipCustomerService>();
            services.AddScoped<ICardTypeService, CardTypeService>();
            services.AddScoped<ICardPackageService, CardPackageService>();
            services.AddScoped<ITemplateCategoryService, TemplateCategoryService>();

            return services;
        }
    }
}
