using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/card-types")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminCardTypesController(ICardTypeService cardTypeService) : ControllerBase
{
    private readonly ICardTypeService _cardTypeService = cardTypeService ?? throw new ArgumentNullException(nameof(cardTypeService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.CardTypes.View)]
    public async Task<IActionResult> GetAllAdminCardTypes([FromQuery] PaginationRequest request, [FromQuery] string? search = null)
    {
        var result = await _cardTypeService.GetAllAdminCardTypesAsync(request, search);
        return Ok(result);
    }

    [HttpGet("export/excel")]
    [HasPermission(AppPermissions.Platform.CardTypes.View)]
    public async Task<IActionResult> ExportExcel([FromQuery] string? search = null)
    {
        var result = await _cardTypeService.ExportCardTypesAsync(ExportFormat.Excel, search);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"CardTypes_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export/pdf")]
    [HasPermission(AppPermissions.Platform.CardTypes.View)]
    public async Task<IActionResult> ExportPdf([FromQuery] string? search = null)
    {
        var result = await _cardTypeService.ExportCardTypesAsync(ExportFormat.Pdf, search);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"CardTypes_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
        return File(result.Data!, "application/pdf", fileName);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardTypes.View)]
    public async Task<IActionResult> GetCardTypeById([FromRoute] Guid id)
    {
        var result = await _cardTypeService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.Platform.CardTypes.Create)]
    public async Task<IActionResult> CreateCardType([FromBody] CreateCardTypeRequest request)
    {
        var result = await _cardTypeService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardTypes.Update)]
    public async Task<IActionResult> UpdateCardType([FromRoute] Guid id, [FromBody] UpdateCardTypeRequest request)
    {
        var result = await _cardTypeService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardTypes.Delete)]
    public async Task<IActionResult> DeleteCardType([FromRoute] Guid id)
    {
        var result = await _cardTypeService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
