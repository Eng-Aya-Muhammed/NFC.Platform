using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using NFC.Platform.API.Controllers.Admin;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.Tests.Controllers;

public class AdminTenantsControllerTests
{
    private readonly IAdminService _adminService = Substitute.For<IAdminService>();
    private readonly ISubscriptionService _subscriptionService = Substitute.For<ISubscriptionService>();
    private readonly AdminTenantsController _sut;

    public AdminTenantsControllerTests()
    {
        _sut = new AdminTenantsController(_adminService, _subscriptionService);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAndRouteAttributes()
    {
        var type = typeof(AdminTenantsController);
        var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal(AppPolicies.AdminOnly, auth.Policy);

        var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().FirstOrDefault();
        Assert.NotNull(route);
        Assert.Equal("api/admin/tenants", route.Template);
    }

    [Theory]
    [InlineData(nameof(AdminTenantsController.GetTenantsPaged), AppPermissions.Platform.Tenants.View)]
    [InlineData(nameof(AdminTenantsController.UpdateTenantStatus), AppPermissions.Platform.Tenants.UpdateStatus)]
    [InlineData(nameof(AdminTenantsController.ExtendSubscription), AppPermissions.Platform.Tenants.ExtendSubscription)]
    public void Endpoints_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(AdminTenantsController).GetMethod(methodName);
        Assert.NotNull(method);

        var auth = method.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal($"Permission:{expectedPermission}", auth.Policy);
    }

    [Fact]
    public async Task GetTenantsPaged_ReturnsOk_OnSuccess()
    {
        var request = new PaginationRequest();
        var pagedResult = PagedResult<TenantSummaryDto>.Create(new List<TenantSummaryDto>(), 0, 1, 10);
        _adminService.GetTenantsPagedAsync(request, Arg.Any<CancellationToken>()).Returns(ServiceResult<PagedResult<TenantSummaryDto>>.Success(pagedResult));

        var result = await _sut.GetTenantsPaged(request, CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task UpdateTenantStatus_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateTenantStatusDto { IsActive = true };
        _adminService.UpdateTenantStatusAsync(id, dto).Returns(ServiceResult.Success());

        var result = await _sut.UpdateTenantStatus(id, dto) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task ExtendSubscription_ReturnsOk_WhenSuccess()
    {
        var tenantId = Guid.NewGuid();
        var req = new ExtendSubscriptionRequest { ExtensionDays = 30, Reason = "Promotion" };
        _subscriptionService.AdminExtendSubscriptionAsync(tenantId, req).Returns(ServiceResult<UserSubscriptionDto>.Success(new UserSubscriptionDto()));

        var result = await _sut.ExtendSubscription(tenantId, req) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
