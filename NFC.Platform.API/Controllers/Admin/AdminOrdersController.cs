using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminOrdersController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

    [HttpGet]
    public async Task<IActionResult> GetOrdersPaged(
        [FromQuery] PaginationRequest request,
        [FromQuery] OrderStatus? status,
        [FromQuery(Name = "company_id")] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetOrdersPagedAsync(request, status, companyId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id)
    {
        var result = await _adminService.GetOrderByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus([FromRoute] Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var result = await _adminService.UpdateOrderStatusAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{id:guid}/verify-otp")]
    public async Task<IActionResult> VerifyDeliveryOtp([FromRoute] Guid id, [FromBody] VerifyDeliveryOtpRequest request)
    {
        var result = await _adminService.VerifyDeliveryOtpAsync(id, request.Otp);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{id:guid}/resend-otp")]
    public async Task<IActionResult> ResendDeliveryOtp([FromRoute] Guid id)
    {
        var result = await _adminService.ResendDeliveryOtpAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
