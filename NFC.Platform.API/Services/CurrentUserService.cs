using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Helpers;

namespace NFC.Platform.API.Services
{
    /// <summary>
    /// Implementation of <see cref="ICurrentUserService"/> using <see cref="IHttpContextAccessor"/> 
    /// to extract user identity claims from the active HTTP request context.
    /// </summary>
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        /// <inheritdoc />
        public Guid? UserId
        {
            get
            {
                var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(AppClaims.UserId);
                if (Guid.TryParse(userIdStr, out Guid userId))
                {
                    return userId;
                }
                
                // Fallback to standard NameIdentifier claim
                var nameIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(nameIdStr, out Guid nameId))
                {
                    return nameId;
                }

                return null;
            }
        }

        /// <inheritdoc />
        public string? Email => 
            _httpContextAccessor.HttpContext?.User?.FindFirstValue(AppClaims.Email) 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

        /// <inheritdoc />
        public bool IsAuthenticated => 
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        /// <inheritdoc />
        public IEnumerable<string> Roles
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null) return [];

                return user.FindAll(ClaimTypes.Role)
                    .Concat(user.FindAll(AppClaims.Role))
                    .Select(c => c.Value)
                    .Distinct();
            }
        }
    }
}
