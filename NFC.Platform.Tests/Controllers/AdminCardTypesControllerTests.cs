using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers.Admin;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers;

public class AdminCardTypesControllerTests
{
    private readonly ICardTypeService _cardTypeService = Substitute.For<ICardTypeService>();
    private readonly AdminCardTypesController _sut;

    public AdminCardTypesControllerTests()
    {
        _sut = new AdminCardTypesController(_cardTypeService);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAndRouteAttributes()
    {
        var type = typeof(AdminCardTypesController);
        var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal(AppPolicies.AdminOnly, auth.Policy);

        var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().FirstOrDefault();
        Assert.NotNull(route);
        Assert.Equal("api/admin/card-types", route.Template);
    }

    [Theory]
    [InlineData(nameof(AdminCardTypesController.GetAllAdminCardTypes), AppPermissions.Platform.CardTypes.View)]
    [InlineData(nameof(AdminCardTypesController.GetCardTypeById), AppPermissions.Platform.CardTypes.View)]
    [InlineData(nameof(AdminCardTypesController.CreateCardType), AppPermissions.Platform.CardTypes.Create)]
    [InlineData(nameof(AdminCardTypesController.UpdateCardType), AppPermissions.Platform.CardTypes.Update)]
    [InlineData(nameof(AdminCardTypesController.DeleteCardType), AppPermissions.Platform.CardTypes.Delete)]
    public void Endpoints_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(AdminCardTypesController).GetMethod(methodName);
        Assert.NotNull(method);

        var auth = method.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal($"Permission:{expectedPermission}", auth.Policy);
    }

    [Fact]
    public async Task GetAllAdminCardTypes_ReturnsOk_OnSuccess()
    {
        var request = new PaginationRequest();
        var pagedResult = PagedResult<CardTypeAdminDto>.Create(new List<CardTypeAdminDto>(), 0, 1, 10);
        _cardTypeService.GetAllAdminCardTypesAsync(request, "Wood").Returns(ServiceResult<PagedResult<CardTypeAdminDto>>.Success(pagedResult));

        var result = await _sut.GetAllAdminCardTypes(request, "Wood") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        await _cardTypeService.Received(1).GetAllAdminCardTypesAsync(request, "Wood");
    }

    [Fact]
    public async Task GetCardTypeById_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var dto = new CardTypeAdminDto { Id = id, NameAr = "نوع كرت", NameEn = "Card Type" };
        _cardTypeService.GetByIdAsync(id).Returns(ServiceResult<CardTypeAdminDto>.Success(dto));

        var result = await _sut.GetCardTypeById(id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetCardTypeById_ReturnsError_WhenNotFound()
    {
        var id = Guid.NewGuid();
        _cardTypeService.GetByIdAsync(id).Returns(ServiceResult<CardTypeAdminDto>.NotFound("Not Found"));

        var result = await _sut.GetCardTypeById(id) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateCardType_ReturnsOk_WhenSuccess()
    {
        var request = new CreateCardTypeRequest { NameAr = "جديد", NameEn = "New" };
        var dto = new CardTypeAdminDto { Id = Guid.NewGuid(), NameAr = "جديد", NameEn = "New" };
        _cardTypeService.CreateAsync(request).Returns(ServiceResult<CardTypeAdminDto>.Success(dto));

        var result = await _sut.CreateCardType(request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task UpdateCardType_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var request = new UpdateCardTypeRequest { NameAr = "معدل", NameEn = "Updated" };
        var dto = new CardTypeAdminDto { Id = id, NameAr = "معدل", NameEn = "Updated" };
        _cardTypeService.UpdateAsync(id, request).Returns(ServiceResult<CardTypeAdminDto>.Success(dto));

        var result = await _sut.UpdateCardType(id, request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task DeleteCardType_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _cardTypeService.DeleteAsync(id).Returns(ServiceResult<bool>.Success(true));

        var result = await _sut.DeleteCardType(id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
