using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;

namespace NFC.Platform.API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminCardTemplatesController(ICardTemplateService cardTemplateService) : ControllerBase
{
    private readonly ICardTemplateService _cardTemplateService = cardTemplateService ?? throw new ArgumentNullException(nameof(cardTemplateService));

    [HttpGet("card-templates")]
    [HasPermission(AppPermissions.Templates.View)]
    public async Task<IActionResult> GetAllAdminCardTemplates([FromQuery] PaginationRequest request)
    {
        var result = await _cardTemplateService.GetAllAdminTemplatesAsync(request);
        return Ok(result);
    }

    [HttpGet("card-templates/{id:guid}")]
    [HasPermission(AppPermissions.Templates.View)]
    public async Task<IActionResult> GetCardTemplateById([FromRoute] Guid id)
    {
        var result = await _cardTemplateService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("card-templates")]
    [HttpPost("templates")]
    [HasPermission(AppPermissions.Templates.Create)]
    public async Task<IActionResult> CreateCardTemplate([FromBody] CreateCardTemplateRequest request)
    {
        var result = await _cardTemplateService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("card-templates/{id:guid}")]
    [HttpPut("templates/{id:guid}")]
    [HasPermission(AppPermissions.Templates.Update)]
    public async Task<IActionResult> UpdateCardTemplate([FromRoute] Guid id, [FromBody] UpdateCardTemplateRequest request)
    {
        var result = await _cardTemplateService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("card-templates/{id:guid}")]
    [HttpDelete("templates/{id:guid}")]
    [HasPermission(AppPermissions.Templates.Delete)]
    public async Task<IActionResult> DeleteCardTemplate([FromRoute] Guid id)
    {
        var result = await _cardTemplateService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
