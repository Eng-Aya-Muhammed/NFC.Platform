

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/templates")]
    [Authorize]
    public class CardTemplateController(
        ICardTemplateService cardTemplateService) : ControllerBase
    {
        private readonly ICardTemplateService _cardTemplateService = cardTemplateService ?? throw new ArgumentNullException(nameof(cardTemplateService));

        [HttpGet]
        public async Task<IActionResult> GetActiveTemplates()
        {
            var result = await _cardTemplateService.GetActiveTemplatesAsync();
            return Ok(result);
        }
    }
}
