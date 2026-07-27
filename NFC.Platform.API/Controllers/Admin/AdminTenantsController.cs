using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/tenants")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminTenantsController(IAdminService adminService, ISubscriptionService subscriptionService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
    private readonly ISubscriptionService _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.Tenants.View)]
    public async Task<IActionResult> GetTenantsPaged([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetTenantsPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [HasPermission(AppPermissions.Platform.Tenants.UpdateStatus)]
    public async Task<IActionResult> UpdateTenantStatus([FromRoute] Guid id, [FromBody] UpdateTenantStatusDto dto)
    {
        var result = await _adminService.UpdateTenantStatusAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{tenantId:guid}/extend-subscription")]
    [HasPermission(AppPermissions.Platform.Tenants.ExtendSubscription)]
    public async Task<IActionResult> ExtendSubscription([FromRoute] Guid tenantId, [FromBody] ExtendSubscriptionRequest request)
    {
        var result = await _subscriptionService.AdminExtendSubscriptionAsync(tenantId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
