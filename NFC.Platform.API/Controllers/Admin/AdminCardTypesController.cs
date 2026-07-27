using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin/card-types")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminCardTypesController(ICardTypeService cardTypeService) : ControllerBase
{
    private readonly ICardTypeService _cardTypeService = cardTypeService ?? throw new ArgumentNullException(nameof(cardTypeService));

    [HttpGet]
    [HasPermission(AppPermissions.CardTypes.View)]
    public async Task<IActionResult> GetAllAdminCardTypes([FromQuery] PaginationRequest request)
    {
        var result = await _cardTypeService.GetAllAdminCardTypesAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.CardTypes.View)]
    public async Task<IActionResult> GetCardTypeById([FromRoute] Guid id)
    {
        var result = await _cardTypeService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.CardTypes.Create)]
    public async Task<IActionResult> CreateCardType([FromBody] CreateCardTypeRequest request)
    {
        var result = await _cardTypeService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.CardTypes.Update)]
    public async Task<IActionResult> UpdateCardType([FromRoute] Guid id, [FromBody] UpdateCardTypeRequest request)
    {
        var result = await _cardTypeService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.CardTypes.Delete)]
    public async Task<IActionResult> DeleteCardType([FromRoute] Guid id)
    {
        var result = await _cardTypeService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
