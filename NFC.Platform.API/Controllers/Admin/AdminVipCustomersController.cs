using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.VipCustomer;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminVipCustomersController(
    IVipCustomerService vipCustomerService,
    ICompanyService companyService,
    IProfileService profileService) : ControllerBase
{
    private readonly IVipCustomerService _vipCustomerService = vipCustomerService ?? throw new ArgumentNullException(nameof(vipCustomerService));
    private readonly ICompanyService _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
    private readonly IProfileService _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

    [HttpGet("vip-customers")]
    [HasPermission(AppPermissions.Platform.VipCustomers.View)]
    public async Task<IActionResult> GetVipCustomers([FromQuery] PaginationRequest request, [FromQuery] string? search = null)
    {
        var result = await _vipCustomerService.GetAdminVipCustomersAsync(request, search);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("companies/{id:guid}/vip")]
    [HasPermission(AppPermissions.Platform.VipCustomers.Update)]
    public async Task<IActionResult> UpdateCompanyVipStatus(Guid id, [FromBody] UpdateVipStatusRequest request)
    {
        var result = await _companyService.UpdateVipStatusAsync(id, request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("profiles/{id:guid}/vip")]
    [HasPermission(AppPermissions.Platform.VipCustomers.Update)]
    public async Task<IActionResult> UpdateProfileVipStatus(Guid id, [FromBody] UpdateVipStatusRequest request)
    {
        var result = await _profileService.UpdateVipStatusAsync(id, request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
