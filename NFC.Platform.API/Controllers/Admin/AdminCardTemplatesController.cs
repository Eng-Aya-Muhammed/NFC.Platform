using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/card-templates")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminCardTemplatesController(ICardTemplateService cardTemplateService) : ControllerBase
{
    private readonly ICardTemplateService _cardTemplateService = cardTemplateService ?? throw new ArgumentNullException(nameof(cardTemplateService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.CardTemplates.View)]
    public async Task<IActionResult> GetAllAdminCardTemplates([FromQuery] PaginationRequest request, [FromQuery] string? search = null)
    {
        var result = await _cardTemplateService.GetAllAdminTemplatesAsync(request, search);
        return Ok(result);
    }

    [HttpGet("export/excel")]
    [HasPermission(AppPermissions.Platform.CardTemplates.View)]
    public async Task<IActionResult> ExportExcel([FromQuery] string? search = null)
    {
        var result = await _cardTemplateService.ExportCardTemplatesAsync(ExportFormat.Excel, search);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"CardTemplates_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export/pdf")]
    [HasPermission(AppPermissions.Platform.CardTemplates.View)]
    public async Task<IActionResult> ExportPdf([FromQuery] string? search = null)
    {
        var result = await _cardTemplateService.ExportCardTemplatesAsync(ExportFormat.Pdf, search);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"CardTemplates_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
        return File(result.Data!, "application/pdf", fileName);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardTemplates.View)]
    public async Task<IActionResult> GetCardTemplateById([FromRoute] Guid id)
    {
        var result = await _cardTemplateService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.Platform.CardTemplates.Create)]
    public async Task<IActionResult> CreateCardTemplate([FromBody] CreateCardTemplateRequest request)
    {
        var result = await _cardTemplateService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardTemplates.Update)]
    public async Task<IActionResult> UpdateCardTemplate([FromRoute] Guid id, [FromBody] UpdateCardTemplateRequest request)
    {
        var result = await _cardTemplateService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardTemplates.Delete)]
    public async Task<IActionResult> DeleteCardTemplate([FromRoute] Guid id)
    {
        var result = await _cardTemplateService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
