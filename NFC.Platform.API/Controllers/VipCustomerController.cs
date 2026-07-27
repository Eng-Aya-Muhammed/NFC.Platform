using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/vip-customers")]
public class VipCustomerController(IVipCustomerService vipCustomerService) : ControllerBase
{
    private readonly IVipCustomerService _vipCustomerService = vipCustomerService ?? throw new ArgumentNullException(nameof(vipCustomerService));

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicVipCustomers()
    {
        var result = await _vipCustomerService.GetPublicVipCustomersAsync();
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
