using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/discount-codes")]
[AllowAnonymous]
public class DiscountCodeController(IDiscountCodeService discountCodeService) : ControllerBase
{
    private readonly IDiscountCodeService _discountCodeService = discountCodeService ?? throw new ArgumentNullException(nameof(discountCodeService));

    /// <summary>
    /// Validates a discount code for customer checkout.
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateDiscountCode([FromBody] ValidateDiscountCodeRequest request)
    {
        var result = await _discountCodeService.ValidateCodeAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
