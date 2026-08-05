using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers.Admin;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers;

public class AdminSubscriptionPlansControllerTests
{
    private readonly IAdminService _adminService = Substitute.For<IAdminService>();
    private readonly AdminSubscriptionPlansController _sut;

    public AdminSubscriptionPlansControllerTests()
    {
        _sut = new AdminSubscriptionPlansController(_adminService);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAndRouteAttributes()
    {
        var type = typeof(AdminSubscriptionPlansController);
        var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal(AppPolicies.AdminOnly, auth.Policy);

        var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().FirstOrDefault();
        Assert.NotNull(route);
        Assert.Equal("api/admin/subscription-plans", route.Template);
    }

    [Theory]
    [InlineData(nameof(AdminSubscriptionPlansController.GetAllAdminPlans), AppPermissions.Platform.SubscriptionPlans.View)]
    [InlineData(nameof(AdminSubscriptionPlansController.GetPlanById), AppPermissions.Platform.SubscriptionPlans.View)]
    [InlineData(nameof(AdminSubscriptionPlansController.CreatePlan), AppPermissions.Platform.SubscriptionPlans.Create)]
    [InlineData(nameof(AdminSubscriptionPlansController.UpdatePlan), AppPermissions.Platform.SubscriptionPlans.Update)]
    [InlineData(nameof(AdminSubscriptionPlansController.DeletePlan), AppPermissions.Platform.SubscriptionPlans.Delete)]
    [InlineData(nameof(AdminSubscriptionPlansController.GetPlanTemplates), AppPermissions.Platform.SubscriptionPlans.View)]
    [InlineData(nameof(AdminSubscriptionPlansController.AssignTemplate), AppPermissions.Platform.SubscriptionPlans.AssignTemplate)]
    [InlineData(nameof(AdminSubscriptionPlansController.UnassignTemplate), AppPermissions.Platform.SubscriptionPlans.AssignTemplate)]
    public void Endpoints_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(AdminSubscriptionPlansController).GetMethod(methodName);
        Assert.NotNull(method);

        var auth = method.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal($"Permission:{expectedPermission}", auth.Policy);
    }

    [Fact]
    public async Task GetAllAdminPlans_ReturnsOk_OnSuccess()
    {
        var request = new PaginationRequest();
        var pagedResult = PagedResult<SubscriptionPlanAdminDto>.Create(new List<SubscriptionPlanAdminDto>(), 0, 1, 10);
        _adminService.GetAllAdminPlansAsync(request, "search-term", Arg.Any<System.Threading.CancellationToken>())
            .Returns(ServiceResult<PagedResult<SubscriptionPlanAdminDto>>.Success(pagedResult));

        var result = await _sut.GetAllAdminPlans(request, "search-term") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
