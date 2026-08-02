using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Services;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CurrentTenantServiceTests
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CurrentTenantService _sut;

        public CurrentTenantServiceTests()
        {
            _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            _sut = new CurrentTenantService(_httpContextAccessor);
        }

        [Fact]
        public void TenantId_ResolvesFromTenantIdClaim_WhenUserIsAuthenticatedNonAdmin()
        {
            // Arrange
            var expectedTenantId = Guid.NewGuid();
            var expectedUserId = Guid.NewGuid();

            var claims = new List<Claim>
            {
                new(AppClaims.TenantId, expectedTenantId.ToString()),
                new(AppClaims.UserId, expectedUserId.ToString()),
                new(AppClaims.Role, AppRole.Customer.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            _httpContextAccessor.HttpContext.Returns(httpContext);

            // Act
            var tenantId = _sut.TenantId;
            var userId = _sut.UserId;

            // Assert
            Assert.Equal(expectedTenantId, tenantId);
            Assert.Equal(expectedUserId, userId);
            Assert.True(_sut.IsAuthenticated);
            Assert.False(_sut.IsAdmin);
        }

        [Fact]
        public void UserId_FallbackToNameIdentifierClaim_WhenUserIdClaimIsMissing()
        {
            // Arrange
            var expectedUserId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
                new(AppClaims.TenantId, Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessor.HttpContext.Returns(httpContext);

            // Act
            var userId = _sut.UserId;

            // Assert
            Assert.Equal(expectedUserId, userId);
        }

        [Fact]
        public void Email_ResolvesFromEmailClaim_AndFallbackToClaimTypesEmail()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, "user@example.com"),
                new(AppClaims.TenantId, Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessor.HttpContext.Returns(httpContext);

            // Act
            var email = _sut.Email;

            // Assert
            Assert.Equal("user@example.com", email);
        }

        [Fact]
        public void AccountType_ResolvesFromAccountTypeClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(AppClaims.AccountType, AccountType.CompanyAdmin.ToString()),
                new(AppClaims.TenantId, Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessor.HttpContext.Returns(httpContext);

            // Act
            var accountType = _sut.AccountType;

            // Assert
            Assert.Equal(AccountType.CompanyAdmin, accountType);
        }

        [Fact]
        public void IsAdmin_ReturnsTrue_WhenUserHasAdminRole()
        {
            // Arrange
            var adminTenantId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(AppClaims.Role, AppRole.Admin.ToString()),
                new(AppClaims.TenantId, adminTenantId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessor.HttpContext.Returns(httpContext);

            // Act
            var isAdmin = _sut.IsAdmin;
            var tenantId = _sut.TenantId;

            // Assert
            Assert.True(isAdmin);
            Assert.Equal(adminTenantId, tenantId);
        }

        [Fact]
        public void TenantId_ThrowsForbiddenException_WhenNonAdminUserLacksTenantIdClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(AppClaims.UserId, Guid.NewGuid().ToString()),
                new(AppClaims.Role, AppRole.Customer.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessor.HttpContext.Returns(httpContext);

            // Act & Assert
            var ex = Assert.Throws<ForbiddenException>(() => _sut.TenantId);
            Assert.Equal("InvalidTenantClaim", ex.Message);
        }

        [Fact]
        public void SetCurrentTenant_OverridesClaimsValues()
        {
            // Arrange
            var tenantOverride = Guid.NewGuid();
            var userOverride = Guid.NewGuid();

            // Act
            _sut.SetCurrentTenant(tenantOverride, userOverride);

            // Assert
            Assert.Equal(tenantOverride, _sut.TenantId);
            Assert.Equal(userOverride, _sut.UserId);
            Assert.True(_sut.IsAuthenticated);
            Assert.Equal("system_job@nfcplatform.com", _sut.Email);
        }

        [Fact]
        public void UnauthenticatedRequest_ReturnsNullValues()
        {
            // Arrange
            _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());

            // Act & Assert
            Assert.Null(_sut.TenantId);
            Assert.Null(_sut.UserId);
            Assert.Null(_sut.Email);
            Assert.Null(_sut.AccountType);
            Assert.False(_sut.IsAuthenticated);
            Assert.False(_sut.IsAdmin);
        }
    }
}
