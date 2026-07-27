using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;
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
    public async Task<IActionResult> GetDiscountCodesPaged([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await _discountCodeService.GetPagedAdminAsync(request, cancellationToken);
        return Ok(result);
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
