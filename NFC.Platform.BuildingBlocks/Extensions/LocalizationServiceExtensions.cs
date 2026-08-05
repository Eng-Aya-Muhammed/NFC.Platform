using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.BuildingBlocks.Extensions
{
    public static class LocalizationServiceExtensions
    {
        public static IServiceCollection AddLocalizationConfig(this IServiceCollection services)
        {
            services.AddLocalization();
            services.AddTransient<IMessageService, MessageService>();
            return services;
        }

        public static IApplicationBuilder UseLocalizationConfig(this IApplicationBuilder app)
        {
            var supportedCultures = new List<CultureInfo>
            {
                new("ar"),
                new("en")
            };

            var options = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("ar"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };

            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());

            app.UseRequestLocalization(options);

            return app;
        }
    }
}
