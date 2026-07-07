using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.BuildingBlocks.Extensions
{
    public static class JwtServiceExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var keyStr = configuration["JwtSettings:Key"] 
                ?? throw new InvalidOperationException("JWT Secret Key 'JwtSettings:Key' is not configured.");

            var key = Encoding.UTF8.GetBytes(keyStr);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AppPolicies.AdminOnly, policy => policy.RequireRole(AppRole.Admin.ToString()));
                options.AddPolicy(AppPolicies.CustomerOnly, policy => policy.RequireRole(AppRole.Customer.ToString()));
            });

            return services;
        }
    }
}
