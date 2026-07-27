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
using NFC.Platform.Application.DTOs.VipCustomer;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.Tests.Controllers;

public class AdminVipCustomersControllerTests
{
    private readonly IVipCustomerService _vipCustomerService = Substitute.For<IVipCustomerService>();
    private readonly ICompanyService _companyService = Substitute.For<ICompanyService>();
    private readonly IProfileService _profileService = Substitute.For<IProfileService>();
    private readonly AdminVipCustomersController _sut;

    public AdminVipCustomersControllerTests()
    {
        _sut = new AdminVipCustomersController(_vipCustomerService, _companyService, _profileService);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAndRouteAttributes()
    {
        var type = typeof(AdminVipCustomersController);
        var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal(AppPolicies.AdminOnly, auth.Policy);

        var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().FirstOrDefault();
        Assert.NotNull(route);
        Assert.Equal("api/admin", route.Template);
    }

    [Theory]
    [InlineData(nameof(AdminVipCustomersController.GetVipCustomers), AppPermissions.Platform.VipCustomers.View)]
    [InlineData(nameof(AdminVipCustomersController.UpdateCompanyVipStatus), AppPermissions.Platform.VipCustomers.Update)]
    [InlineData(nameof(AdminVipCustomersController.UpdateProfileVipStatus), AppPermissions.Platform.VipCustomers.Update)]
    public void Endpoints_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(AdminVipCustomersController).GetMethod(methodName);
        Assert.NotNull(method);

        var auth = method.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal($"Permission:{expectedPermission}", auth.Policy);
    }

    [Fact]
    public async Task GetVipCustomers_ReturnsOk_OnSuccess()
    {
        var request = new PaginationRequest();
        var pagedResult = PagedResult<VipCustomerDto>.Create(new List<VipCustomerDto>(), 0, 1, 10);
        _vipCustomerService.GetAdminVipCustomersAsync(request).Returns(ServiceResult<PagedResult<VipCustomerDto>>.Success(pagedResult));

        var result = await _sut.GetVipCustomers(request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task UpdateCompanyVipStatus_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var req = new UpdateVipStatusRequest { IsVip = true };
        _companyService.UpdateVipStatusAsync(id, req).Returns(ServiceResult<VipCustomerDto>.Success(new VipCustomerDto()));

        var result = await _sut.UpdateCompanyVipStatus(id, req) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task UpdateProfileVipStatus_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var req = new UpdateVipStatusRequest { IsVip = true };
        _profileService.UpdateVipStatusAsync(id, req).Returns(ServiceResult<VipCustomerDto>.Success(new VipCustomerDto()));

        var result = await _sut.UpdateProfileVipStatus(id, req) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
