using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers.Admin;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers;

public class AdminSubdomainsControllerTests
{
    private readonly IAdminService _adminService = Substitute.For<IAdminService>();
    private readonly IMessageService _msg = Substitute.For<IMessageService>();
    private readonly AdminSubdomainsController _sut;

    public AdminSubdomainsControllerTests()
    {
        _sut = new AdminSubdomainsController(_adminService, _msg);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAndRouteAttributes()
    {
        var type = typeof(AdminSubdomainsController);
        var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal(AppPolicies.AdminOnly, auth.Policy);

        var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().FirstOrDefault();
        Assert.NotNull(route);
        Assert.Equal("api/admin/subdomains", route.Template);
    }

    [Theory]
    [InlineData(nameof(AdminSubdomainsController.GetSubdomains), AppPermissions.Platform.Subdomains.View)]
    [InlineData(nameof(AdminSubdomainsController.ReassignSubdomain), AppPermissions.Platform.Subdomains.Update)]
    public void Endpoints_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(AdminSubdomainsController).GetMethod(methodName);
        Assert.NotNull(method);

        var auth = method.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal($"Permission:{expectedPermission}", auth.Policy);
    }

    [Fact]
    public async Task GetSubdomains_ReturnsOk_OnSuccess()
    {
        var request = new PaginationRequest();
        var pagedResult = PagedResult<ProfileSubdomainSummaryDto>.Create(new List<ProfileSubdomainSummaryDto>(), 0, 1, 10);
        _adminService.GetSubdomainsPagedAsync(request, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>.Success(pagedResult));

        var result = await _sut.GetSubdomains(request, null, CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetSubdomains_ReturnsStatusCode_OnFailure()
    {
        var request = new PaginationRequest();
        _adminService.GetSubdomainsPagedAsync(request, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>.Fail("Error", 500));

        var result = await _sut.GetSubdomains(request, null, CancellationToken.None) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task ReassignSubdomain_ReturnsOk_WhenSuccess()
    {
        var profileId = Guid.NewGuid();
        var dto = new ReassignSubdomainDto { Subdomain = "new-slug" };
        _adminService.ReassignSubdomainAsync(profileId, dto).Returns(ServiceResult.Success());

        var result = await _sut.ReassignSubdomain(profileId, dto) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task ReassignSubdomain_ReturnsStatusCode_WhenFailure()
    {
        var profileId = Guid.NewGuid();
        var dto = new ReassignSubdomainDto { Subdomain = "taken-slug" };
        _adminService.ReassignSubdomainAsync(profileId, dto).Returns(ServiceResult.Fail("Already taken", 409));

        var result = await _sut.ReassignSubdomain(profileId, dto) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(409, result.StatusCode);
    }
}
