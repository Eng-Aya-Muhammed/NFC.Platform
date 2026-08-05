using System;
using System.Security.Claims;
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
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = AppClaims.UserId,
                    RoleClaimType = AppClaims.Role
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AppPolicies.AdminOnly, policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.IsInRole(AppRole.Admin.ToString()) ||
                        ctx.User.HasClaim(c => (c.Type == AppClaims.Role || c.Type == ClaimTypes.Role) && c.Value == AppRole.Admin.ToString())));

                options.AddPolicy(AppPolicies.CompanyAdminOnly, policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.IsInRole(AppRole.CompanyAdmin.ToString()) ||
                        ctx.User.HasClaim(c => (c.Type == AppClaims.Role || c.Type == ClaimTypes.Role) && c.Value == AppRole.CompanyAdmin.ToString())));
            });

            services.AddMemoryCache();

            return services;
        }
    }
}
