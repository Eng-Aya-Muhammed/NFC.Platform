using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/tenants")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminTenantsController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

    [HttpGet]
    public async Task<IActionResult> GetTenantsPaged([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetTenantsPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateTenantStatus([FromRoute] Guid id, [FromBody] UpdateTenantStatusDto dto)
    {
        var result = await _adminService.UpdateTenantStatusAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
