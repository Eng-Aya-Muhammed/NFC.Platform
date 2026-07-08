using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;

namespace NFC.Platform.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GenerateToken(Guid userId, string email, IEnumerable<string> roles, Guid tenantId, Guid? companyId = null, string? accountType = null)
        {
            var keyStr = _configuration["JwtSettings:Key"] 
                ?? throw new InvalidOperationException("JWT Secret Key 'JwtSettings:Key' is not configured.");

            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var expiryInMinutes = double.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(AppClaims.UserId, userId.ToString()),
                new Claim(AppClaims.Email, email),
                new Claim(AppClaims.TenantId, tenantId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (roles != null)
            {
                claims.AddRange(roles.Select(role => new Claim(AppClaims.Role, role)));
            }

            if (companyId.HasValue)
            {
                claims.Add(new Claim(AppClaims.CompanyId, companyId.Value.ToString()));
            }

            if (!string.IsNullOrEmpty(accountType))
            {
                claims.Add(new Claim(AppClaims.AccountType, accountType));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

