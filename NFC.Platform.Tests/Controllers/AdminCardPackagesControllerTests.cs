using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using NFC.Platform.API.Controllers.Admin;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardPackage;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.Tests.Controllers;

public class AdminCardPackagesControllerTests
{
    private readonly ICardPackageService _cardPackageService = Substitute.For<ICardPackageService>();
    private readonly AdminCardPackagesController _sut;

    public AdminCardPackagesControllerTests()
    {
        _sut = new AdminCardPackagesController(_cardPackageService);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAndRouteAttributes()
    {
        var type = typeof(AdminCardPackagesController);
        var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal(AppPolicies.AdminOnly, auth.Policy);

        var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().FirstOrDefault();
        Assert.NotNull(route);
        Assert.Equal("api/admin/card-packages", route.Template);
    }

    [Theory]
    [InlineData(nameof(AdminCardPackagesController.GetAllAdminCardPackages), AppPermissions.Platform.CardPackages.View)]
    [InlineData(nameof(AdminCardPackagesController.GetCardPackageById), AppPermissions.Platform.CardPackages.View)]
    [InlineData(nameof(AdminCardPackagesController.CreateCardPackage), AppPermissions.Platform.CardPackages.Create)]
    [InlineData(nameof(AdminCardPackagesController.UpdateCardPackage), AppPermissions.Platform.CardPackages.Update)]
    [InlineData(nameof(AdminCardPackagesController.DeleteCardPackage), AppPermissions.Platform.CardPackages.Delete)]
    public void Endpoints_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(AdminCardPackagesController).GetMethod(methodName);
        Assert.NotNull(method);

        var auth = method.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal($"Permission:{expectedPermission}", auth.Policy);
    }

    [Fact]
    public async Task GetAllAdminCardPackages_ReturnsOk_OnSuccess()
    {
        var request = new PaginationRequest();
        var pagedResult = PagedResult<CardPackageAdminDto>.Create(new List<CardPackageAdminDto>(), 0, 1, 10);
        _cardPackageService.GetAllAdminCardPackagesAsync(request, "50").Returns(ServiceResult<PagedResult<CardPackageAdminDto>>.Success(pagedResult));

        var result = await _sut.GetAllAdminCardPackages(request, "50") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task CreateCardPackage_ReturnsOk_WhenSuccess()
    {
        var request = new CreateCardPackageRequest();
        var dto = new CardPackageAdminDto();
        _cardPackageService.CreateAsync(request).Returns(ServiceResult<CardPackageAdminDto>.Success(dto));

        var result = await _sut.CreateCardPackage(request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
