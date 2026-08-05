using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

/// <summary>
/// Super Admin endpoints for managing user profile subdomains (public URL slugs).
/// </summary>
[ApiController]
[Route("api/admin/subdomains")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminSubdomainsController(IAdminService adminService, IMessageService msg) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
    private readonly IMessageService _msg = msg ?? throw new ArgumentNullException(nameof(msg));

    /// <summary>
    /// Returns a paginated list of all profile subdomains for Super Admin oversight.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Platform.Subdomains.View)]
    public async Task<IActionResult> GetSubdomains(
        [FromQuery] PaginationRequest request,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetSubdomainsPagedAsync(request, search, cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>
    /// Reassigns a custom subdomain slug to the specified user profile.
    /// Slug must match: ^[a-z0-9][a-z0-9\-]{0,98}[a-z0-9]$
    /// </summary>
    [HttpPut("{profileId:guid}")]
    [HasPermission(AppPermissions.Platform.Subdomains.Update)]
    public async Task<IActionResult> ReassignSubdomain(
        [FromRoute] Guid profileId,
        [FromBody] ReassignSubdomainDto dto)
    {
        var result = await _adminService.ReassignSubdomainAsync(profileId, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
