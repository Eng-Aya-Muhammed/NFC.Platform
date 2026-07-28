using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Enums;
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

    [HttpGet("export/excel")]
    [HasPermission(AppPermissions.Platform.TemplateCategories.View)]
    public async Task<IActionResult> ExportExcel()
    {
        var result = await _templateCategoryService.ExportTemplateCategoriesAsync(ExportFormat.Excel);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"TemplateCategories_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export/pdf")]
    [HasPermission(AppPermissions.Platform.TemplateCategories.View)]
    public async Task<IActionResult> ExportPdf()
    {
        var result = await _templateCategoryService.ExportTemplateCategoriesAsync(ExportFormat.Pdf);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"TemplateCategories_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
        return File(result.Data!, "application/pdf", fileName);
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
