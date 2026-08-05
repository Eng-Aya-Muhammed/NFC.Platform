using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/discount-codes")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminDiscountCodesController(IDiscountCodeService discountCodeService) : ControllerBase
{
    private readonly IDiscountCodeService _discountCodeService = discountCodeService ?? throw new ArgumentNullException(nameof(discountCodeService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.DiscountCodes.View)]
    public async Task<IActionResult> GetDiscountCodesPaged([FromQuery] PaginationRequest request, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _discountCodeService.GetPagedAdminAsync(request, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export/excel")]
    [HasPermission(AppPermissions.Platform.DiscountCodes.View)]
    public async Task<IActionResult> ExportExcel([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _discountCodeService.ExportDiscountCodesAsync(ExportFormat.Excel, search, cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"DiscountCodes_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export/pdf")]
    [HasPermission(AppPermissions.Platform.DiscountCodes.View)]
    public async Task<IActionResult> ExportPdf([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _discountCodeService.ExportDiscountCodesAsync(ExportFormat.Pdf, search, cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"DiscountCodes_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
        return File(result.Data!, "application/pdf", fileName);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.Platform.DiscountCodes.View)]
    public async Task<IActionResult> GetDiscountCodeById([FromRoute] Guid id)
    {
        var result = await _discountCodeService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.Platform.DiscountCodes.Create)]
    public async Task<IActionResult> CreateDiscountCode([FromBody] CreateDiscountCodeRequest request)
    {
        var result = await _discountCodeService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.Platform.DiscountCodes.Update)]
    public async Task<IActionResult> UpdateDiscountCode([FromRoute] Guid id, [FromBody] UpdateDiscountCodeRequest request)
    {
        var result = await _discountCodeService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.Platform.DiscountCodes.Delete)]
    public async Task<IActionResult> DeleteDiscountCode([FromRoute] Guid id)
    {
        var result = await _discountCodeService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
