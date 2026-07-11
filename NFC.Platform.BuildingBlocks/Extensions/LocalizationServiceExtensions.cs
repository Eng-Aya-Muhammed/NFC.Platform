using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using NFC.Platform.BuildingBlocks.Localization;

namespace NFC.Platform.BuildingBlocks.Extensions
{
    /// <summary>
    /// Service registration and request pipeline configuration extensions for application localization.
    /// </summary>
    public static class LocalizationServiceExtensions
    {
        /// <summary>
        /// Registers ASP.NET Core localization services and the custom <see cref="IMessageService"/> wrapper.
        /// </summary>
        /// <param name="services">The service collection descriptor.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddLocalizationConfig(this IServiceCollection services)
        {
            services.AddLocalization();
            services.AddTransient<IMessageService, MessageService>();
            return services;
        }

        /// <summary>
        /// Configures and enables the request localization middleware, supporting 'ar' and 'en' cultures with 'ar' as the default.
        /// Prioritizes the HTTP 'Accept-Language' header.
        /// </summary>
        /// <param name="app">The application builder pipeline instance.</param>
        /// <returns>The modified application builder.</returns>
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

            // Prioritize the Accept-Language header by clearing other providers and setting it as the sole provider
            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());

            app.UseRequestLocalization(options);

            return app;
        }
    }
}
