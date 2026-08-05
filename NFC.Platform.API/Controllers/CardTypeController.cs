namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/card-types")]
[Authorize]
public class CardTypeController(ICardTypeService cardTypeService) : ControllerBase
{
    private readonly ICardTypeService _cardTypeService = cardTypeService ?? throw new ArgumentNullException(nameof(cardTypeService));

    [HttpGet]
    public async Task<IActionResult> GetActiveCardTypes([FromQuery] string? search = null)
    {
        var result = await _cardTypeService.GetActiveCardTypesAsync(search);
        return Ok(result);
    }
}
