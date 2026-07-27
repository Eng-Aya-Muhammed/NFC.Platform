using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/template-categories")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminTemplateCategoriesController(ITemplateCategoryService templateCategoryService) : ControllerBase
{
    private readonly ITemplateCategoryService _templateCategoryService = templateCategoryService ?? throw new ArgumentNullException(nameof(templateCategoryService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.TemplateCategories.View)]
    public async Task<IActionResult> GetAllAdminTemplateCategories([FromQuery] PaginationRequest request)
    {
        var result = await _templateCategoryService.GetAllAdminCategoriesAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.Platform.TemplateCategories.View)]
    public async Task<IActionResult> GetTemplateCategoryById([FromRoute] Guid id)
    {
        var result = await _templateCategoryService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.Platform.TemplateCategories.Create)]
    public async Task<IActionResult> CreateTemplateCategory([FromBody] CreateTemplateCategoryRequest request)
    {
        var result = await _templateCategoryService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.Platform.TemplateCategories.Update)]
    public async Task<IActionResult> UpdateTemplateCategory([FromRoute] Guid id, [FromBody] UpdateTemplateCategoryRequest request)
    {
        var result = await _templateCategoryService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.Platform.TemplateCategories.Delete)]
    public async Task<IActionResult> DeleteTemplateCategory([FromRoute] Guid id)
    {
        var result = await _templateCategoryService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
