using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/card-templates")]
[Authorize]
public class CardTemplateController(ICardTemplateService cardTemplateService) : ControllerBase
{
    private readonly ICardTemplateService _cardTemplateService = cardTemplateService ?? throw new ArgumentNullException(nameof(cardTemplateService));

    [HttpGet]
    public async Task<IActionResult> GetActiveTemplates([FromQuery] string? search = null)
    {
        var result = await _cardTemplateService.GetActiveTemplatesAsync(search);
        return Ok(result);
    }
}
