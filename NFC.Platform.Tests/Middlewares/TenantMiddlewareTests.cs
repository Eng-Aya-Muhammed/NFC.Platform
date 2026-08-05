using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;
using NFC.Platform.API.Middlewares;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Infrastructure.Contexts;
using NFC.Platform.Infrastructure.Interceptors;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Middlewares
{
    public class TenantMiddlewareTests
    {
        private readonly RequestDelegate _next;
        private readonly TenantMiddleware _middleware;
        private bool _nextCalled;

        public TenantMiddlewareTests()
        {
            _next = (ctx) =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            };
            _middleware = new TenantMiddleware(_next);
        }

        private static ApplicationDbContext CreateMockDbContext(List<Tenant> tenants)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            var currentUserService = Substitute.For<ICurrentUserService>();
            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            var interceptor = new AuditableEntitySaveChangesInterceptor(currentUserService, dateTimeProvider);
            var currentTenant = Substitute.For<ICurrentTenant>();

            var context = Substitute.For<ApplicationDbContext>(options, interceptor, currentTenant);
            var mockDbSet = tenants.AsQueryable().BuildMockDbSet();
            context.Set<Tenant>().Returns(mockDbSet);

            return context;
        }

        [Fact]
        public async Task InvokeAsync_CallsNextMiddleware_WhenRequestIsUnauthenticated()
        {
            var httpContext = new DefaultHttpContext();
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.IsAuthenticated.Returns(false);
            var dbContext = CreateMockDbContext(new List<Tenant>());

            await _middleware.InvokeAsync(httpContext, currentTenant, dbContext);

            Assert.True(_nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_CallsNextMiddleware_WhenUserIsAdmin()
        {
            var httpContext = new DefaultHttpContext();
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.IsAuthenticated.Returns(true);
            currentTenant.IsAdmin.Returns(true);
            var dbContext = CreateMockDbContext(new List<Tenant>());

            await _middleware.InvokeAsync(httpContext, currentTenant, dbContext);

            Assert.True(_nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_CallsNextMiddleware_WhenTenantIsActiveInDatabase()
        {
            var tenantId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.IsAuthenticated.Returns(true);
            currentTenant.IsAdmin.Returns(false);
            currentTenant.TenantId.Returns(tenantId);

            var tenants = new List<Tenant>
            {
                new Tenant
                {
                    Id = tenantId,
                    Name = "Active Tenant",
                    IsActive = true
                }
            };
            var dbContext = CreateMockDbContext(tenants);

            await _middleware.InvokeAsync(httpContext, currentTenant, dbContext);

            Assert.True(_nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_ThrowsForbiddenException_TenantNotFound_WhenTenantDoesNotExist()
        {
            var tenantId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.IsAuthenticated.Returns(true);
            currentTenant.IsAdmin.Returns(false);
            currentTenant.TenantId.Returns(tenantId);

            var dbContext = CreateMockDbContext(new List<Tenant>());

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                _middleware.InvokeAsync(httpContext, currentTenant, dbContext));

            Assert.Equal("TenantNotFound", ex.Message);
            Assert.False(_nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_ThrowsForbiddenException_TenantInactive_WhenTenantIsDisabled()
        {
            var tenantId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.IsAuthenticated.Returns(true);
            currentTenant.IsAdmin.Returns(false);
            currentTenant.TenantId.Returns(tenantId);

            var tenants = new List<Tenant>
            {
                new Tenant
                {
                    Id = tenantId,
                    Name = "Disabled Tenant",
                    IsActive = false
                }
            };
            var dbContext = CreateMockDbContext(tenants);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                _middleware.InvokeAsync(httpContext, currentTenant, dbContext));

            Assert.Equal("TenantInactive", ex.Message);
            Assert.False(_nextCalled);
        }
    }
}
