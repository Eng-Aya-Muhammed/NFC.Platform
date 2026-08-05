using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardPackage;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/card-packages")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminCardPackagesController(ICardPackageService cardPackageService) : ControllerBase
{
    private readonly ICardPackageService _cardPackageService = cardPackageService ?? throw new ArgumentNullException(nameof(cardPackageService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.CardPackages.View)]
    public async Task<IActionResult> GetAllAdminCardPackages([FromQuery] PaginationRequest request, [FromQuery] string? search = null)
    {
        var result = await _cardPackageService.GetAllAdminCardPackagesAsync(request, search);
        return Ok(result);
    }

    [HttpGet("export/excel")]
    [HasPermission(AppPermissions.Platform.CardPackages.View)]
    public async Task<IActionResult> ExportExcel([FromQuery] string? search = null)
    {
        var result = await _cardPackageService.ExportCardPackagesAsync(ExportFormat.Excel, search);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"CardPackages_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export/pdf")]
    [HasPermission(AppPermissions.Platform.CardPackages.View)]
    public async Task<IActionResult> ExportPdf([FromQuery] string? search = null)
    {
        var result = await _cardPackageService.ExportCardPackagesAsync(ExportFormat.Pdf, search);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"CardPackages_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
        return File(result.Data!, "application/pdf", fileName);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardPackages.View)]
    public async Task<IActionResult> GetCardPackageById([FromRoute] Guid id)
    {
        var result = await _cardPackageService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.Platform.CardPackages.Create)]
    public async Task<IActionResult> CreateCardPackage([FromBody] CreateCardPackageRequest request)
    {
        var result = await _cardPackageService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardPackages.Update)]
    public async Task<IActionResult> UpdateCardPackage([FromRoute] Guid id, [FromBody] UpdateCardPackageRequest request)
    {
        var result = await _cardPackageService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.Platform.CardPackages.Delete)]
    public async Task<IActionResult> DeleteCardPackage([FromRoute] Guid id)
    {
        var result = await _cardPackageService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
