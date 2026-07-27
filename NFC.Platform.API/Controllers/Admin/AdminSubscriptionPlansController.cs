using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminSubscriptionPlansController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

    [HttpGet("subscription-plans")]
    [HttpGet("plans")]
    public async Task<IActionResult> GetAllAdminPlans([FromQuery] PaginationRequest request)
    {
        var result = await _adminService.GetAllAdminPlansAsync(request);
        return Ok(result);
    }

    [HttpGet("subscription-plans/{id:guid}")]
    [HttpGet("plans/{id:guid}")]
    public async Task<IActionResult> GetPlanById([FromRoute] Guid id)
    {
        var result = await _adminService.GetPlanByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("subscription-plans")]
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequest request)
    {
        var result = await _adminService.CreatePlanAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("subscription-plans/{planId:guid}")]
    [HttpPut("plans/{planId:guid}")]
    public async Task<IActionResult> UpdatePlan([FromRoute] Guid planId, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        var result = await _adminService.UpdatePlanAsync(planId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("subscription-plans/{planId:guid}")]
    [HttpDelete("plans/{planId:guid}")]
    public async Task<IActionResult> DeletePlan([FromRoute] Guid planId)
    {
        var result = await _adminService.DeletePlanAsync(planId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("plans/{planId:guid}/templates")]
    public async Task<IActionResult> GetPlanTemplates([FromRoute] Guid planId)
    {
        var result = await _adminService.GetPlanTemplatesAsync(planId);
        return Ok(result);
    }

    [HttpPost("plans/{planId:guid}/templates/{templateId:guid}")]
    public async Task<IActionResult> AssignTemplate([FromRoute] Guid planId, [FromRoute] Guid templateId)
    {
        var result = await _adminService.AssignTemplateAsync(planId, templateId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("plans/{planId:guid}/templates/{templateId:guid}")]
    public async Task<IActionResult> UnassignTemplate([FromRoute] Guid planId, [FromRoute] Guid templateId)
    {
        var result = await _adminService.UnassignTemplateAsync(planId, templateId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
