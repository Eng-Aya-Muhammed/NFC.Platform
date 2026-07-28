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
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminOrdersController(IAdminService adminService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

    [HttpGet]
    [HasPermission(AppPermissions.Platform.Orders.View)]
    public async Task<IActionResult> GetOrdersPaged(
        [FromQuery] PaginationRequest request,
        [FromQuery] OrderStatus? status,
        [FromQuery(Name = "company_id")] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetOrdersPagedAsync(request, status, companyId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export/excel")]
    [HasPermission(AppPermissions.Platform.Orders.View)]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] OrderStatus? status,
        [FromQuery(Name = "company_id")] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.ExportAdminOrdersAsync(NFC.Platform.BuildingBlocks.Common.Models.ExportFormat.Excel, status, companyId, cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"AdminOrders_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export/pdf")]
    [HasPermission(AppPermissions.Platform.Orders.View)]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] OrderStatus? status,
        [FromQuery(Name = "company_id")] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.ExportAdminOrdersAsync(NFC.Platform.BuildingBlocks.Common.Models.ExportFormat.Pdf, status, companyId, cancellationToken);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"AdminOrders_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
        return File(result.Data!, "application/pdf", fileName);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.Platform.Orders.View)]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id)
    {
        var result = await _adminService.GetOrderByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [HasPermission(AppPermissions.Platform.Orders.UpdateStatus)]
    public async Task<IActionResult> UpdateOrderStatus([FromRoute] Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var result = await _adminService.UpdateOrderStatusAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{id:guid}/verify-otp")]
    [HasPermission(AppPermissions.Platform.Orders.VerifyOtp)]
    public async Task<IActionResult> VerifyDeliveryOtp([FromRoute] Guid id, [FromBody] VerifyDeliveryOtpRequest request)
    {
        var result = await _adminService.VerifyDeliveryOtpAsync(id, request.Otp);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{id:guid}/resend-otp")]
    [HasPermission(AppPermissions.Platform.Orders.ResendOtp)]
    public async Task<IActionResult> ResendDeliveryOtp([FromRoute] Guid id)
    {
        var result = await _adminService.ResendDeliveryOtpAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
