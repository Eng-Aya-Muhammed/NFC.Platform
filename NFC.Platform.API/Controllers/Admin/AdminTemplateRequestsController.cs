using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/template-requests")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminTemplateRequestsController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.TemplateRequests.View)]
    public async Task<IActionResult> GetTemplateRequestsPaged([FromQuery] PaginationRequest request, [FromQuery] TemplateRequestStatus? status, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetTemplateRequestsPagedAsync(request, status, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/resolve")]
    [HasPermission(AppPermissions.Platform.TemplateRequests.Resolve)]
    public async Task<IActionResult> ResolveTemplateRequest([FromRoute] Guid id, [FromBody] ResolveTemplateRequestDto dto)
    {
        var result = await _adminService.ResolveTemplateRequestAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
