using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/subscription-plans")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminSubscriptionPlansController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.SubscriptionPlans.View)]
    public async Task<IActionResult> GetAllAdminPlans([FromQuery] PaginationRequest request, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetAllAdminPlansAsync(request, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.Platform.SubscriptionPlans.View)]
    public async Task<IActionResult> GetPlanById([FromRoute] Guid id)
    {
        var result = await _adminService.GetPlanByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.Platform.SubscriptionPlans.Create)]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequest request)
    {
        var result = await _adminService.CreatePlanAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{planId:guid}")]
    [HasPermission(AppPermissions.Platform.SubscriptionPlans.Update)]
    public async Task<IActionResult> UpdatePlan([FromRoute] Guid planId, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        var result = await _adminService.UpdatePlanAsync(planId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{planId:guid}")]
    [HasPermission(AppPermissions.Platform.SubscriptionPlans.Delete)]
    public async Task<IActionResult> DeletePlan([FromRoute] Guid planId)
    {
        var result = await _adminService.DeletePlanAsync(planId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{planId:guid}/templates")]
    [HasPermission(AppPermissions.Platform.SubscriptionPlans.View)]
    public async Task<IActionResult> GetPlanTemplates([FromRoute] Guid planId)
    {
        var result = await _adminService.GetPlanTemplatesAsync(planId);
        return Ok(result);
    }

    [HttpPost("{planId:guid}/templates/{templateId:guid}")]
    [HasPermission(AppPermissions.Platform.SubscriptionPlans.AssignTemplate)]
    public async Task<IActionResult> AssignTemplate([FromRoute] Guid planId, [FromRoute] Guid templateId)
    {
        var result = await _adminService.AssignTemplateAsync(planId, templateId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{planId:guid}/templates/{templateId:guid}")]
    [HasPermission(AppPermissions.Platform.SubscriptionPlans.AssignTemplate)]
    public async Task<IActionResult> UnassignTemplate([FromRoute] Guid planId, [FromRoute] Guid templateId)
    {
        var result = await _adminService.UnassignTemplateAsync(planId, templateId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
