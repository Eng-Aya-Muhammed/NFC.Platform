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
    public async Task<IActionResult> GetTenantsPaged([FromQuery] PaginationRequest request, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetTenantsPagedAsync(request, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export/excel")]
    [HasPermission(AppPermissions.Platform.Tenants.View)]
    public async Task<IActionResult> ExportExcel([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _adminService.ExportTenantsAsync(NFC.Platform.BuildingBlocks.Common.Models.ExportFormat.Excel, search, cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"Tenants_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export/pdf")]
    [HasPermission(AppPermissions.Platform.Tenants.View)]
    public async Task<IActionResult> ExportPdf([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await _adminService.ExportTenantsAsync(NFC.Platform.BuildingBlocks.Common.Models.ExportFormat.Pdf, search, cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"Tenants_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
        return File(result.Data!, "application/pdf", fileName);
    }

    [HttpPut("{id:guid}/status")]
    [HasPermission(AppPermissions.Platform.Tenants.UpdateStatus)]
    public async Task<IActionResult> UpdateTenantStatus([FromRoute] Guid id, [FromBody] UpdateTenantStatusDto dto)
    {
        var result = await _adminService.UpdateTenantStatusAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{tenantId:guid}/basic-info")]
    [HasPermission(AppPermissions.Platform.Tenants.View)]
    public async Task<IActionResult> GetTenantBasicInfo([FromRoute] Guid tenantId)
    {
        var result = await _adminService.GetTenantBasicInfoAsync(tenantId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{tenantId:guid}/employees")]
    [HasPermission(AppPermissions.Platform.Tenants.View)]
    public async Task<IActionResult> GetTenantEmployees([FromRoute] Guid tenantId, [FromQuery] PaginationRequest request, [FromQuery] string? search = null)
    {
        var result = await _adminService.GetTenantEmployeesPagedAsync(tenantId, request, search);
        return Ok(result);
    }

    [HttpGet("{tenantId:guid}/employees/{employeeId:guid}")]
    [HasPermission(AppPermissions.Platform.Tenants.View)]
    public async Task<IActionResult> GetTenantEmployeeDetails([FromRoute] Guid tenantId, [FromRoute] Guid employeeId)
    {
        var result = await _adminService.GetTenantEmployeeDetailsAsync(tenantId, employeeId);
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
