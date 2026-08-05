using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers.Admin;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers;

public class AdminTemplateCategoriesControllerTests
{
    private readonly ITemplateCategoryService _templateCategoryService = Substitute.For<ITemplateCategoryService>();
    private readonly AdminTemplateCategoriesController _sut;

    public AdminTemplateCategoriesControllerTests()
    {
        _sut = new AdminTemplateCategoriesController(_templateCategoryService);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAndRouteAttributes()
    {
        var type = typeof(AdminTemplateCategoriesController);
        var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal(AppPolicies.AdminOnly, auth.Policy);

        var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().FirstOrDefault();
        Assert.NotNull(route);
        Assert.Equal("api/admin/template-categories", route.Template);
    }

    [Theory]
    [InlineData(nameof(AdminTemplateCategoriesController.GetAllAdminTemplateCategories), AppPermissions.Platform.TemplateCategories.View)]
    [InlineData(nameof(AdminTemplateCategoriesController.GetTemplateCategoryById), AppPermissions.Platform.TemplateCategories.View)]
    [InlineData(nameof(AdminTemplateCategoriesController.CreateTemplateCategory), AppPermissions.Platform.TemplateCategories.Create)]
    [InlineData(nameof(AdminTemplateCategoriesController.UpdateTemplateCategory), AppPermissions.Platform.TemplateCategories.Update)]
    [InlineData(nameof(AdminTemplateCategoriesController.DeleteTemplateCategory), AppPermissions.Platform.TemplateCategories.Delete)]
    public void Endpoints_ShouldHaveCorrectPermissionAttributes(string methodName, string expectedPermission)
    {
        var method = typeof(AdminTemplateCategoriesController).GetMethod(methodName);
        Assert.NotNull(method);

        var auth = method.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
        Assert.NotNull(auth);
        Assert.Equal($"Permission:{expectedPermission}", auth.Policy);
    }

    [Fact]
    public async Task GetAllAdminTemplateCategories_ReturnsOk_OnSuccess()
    {
        var request = new PaginationRequest();
        var pagedResult = PagedResult<TemplateCategoryAdminDto>.Create(new List<TemplateCategoryAdminDto>(), 0, 1, 10);
        _templateCategoryService.GetAllAdminCategoriesAsync(request).Returns(ServiceResult<PagedResult<TemplateCategoryAdminDto>>.Success(pagedResult));

        var result = await _sut.GetAllAdminTemplateCategories(request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetTemplateCategoryById_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var dto = new TemplateCategoryAdminDto { Id = id, NameAr = "تصنيف", NameEn = "Category" };
        _templateCategoryService.GetByIdAsync(id).Returns(ServiceResult<TemplateCategoryAdminDto>.Success(dto));

        var result = await _sut.GetTemplateCategoryById(id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetTemplateCategoryById_ReturnsError_WhenNotFound()
    {
        var id = Guid.NewGuid();
        _templateCategoryService.GetByIdAsync(id).Returns(ServiceResult<TemplateCategoryAdminDto>.NotFound("Not Found"));

        var result = await _sut.GetTemplateCategoryById(id) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateTemplateCategory_ReturnsOk_WhenSuccess()
    {
        var request = new CreateTemplateCategoryRequest { NameAr = "جديد", NameEn = "New" };
        var dto = new TemplateCategoryAdminDto { Id = Guid.NewGuid(), NameAr = "جديد", NameEn = "New" };
        _templateCategoryService.CreateAsync(request).Returns(ServiceResult<TemplateCategoryAdminDto>.Success(dto));

        var result = await _sut.CreateTemplateCategory(request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task UpdateTemplateCategory_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var request = new UpdateTemplateCategoryRequest { NameAr = "معدل", NameEn = "Updated" };
        var dto = new TemplateCategoryAdminDto { Id = id, NameAr = "معدل", NameEn = "Updated" };
        _templateCategoryService.UpdateAsync(id, request).Returns(ServiceResult<TemplateCategoryAdminDto>.Success(dto));

        var result = await _sut.UpdateTemplateCategory(id, request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task DeleteTemplateCategory_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _templateCategoryService.DeleteAsync(id).Returns(ServiceResult<bool>.Success(true));

        var result = await _sut.DeleteTemplateCategory(id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
