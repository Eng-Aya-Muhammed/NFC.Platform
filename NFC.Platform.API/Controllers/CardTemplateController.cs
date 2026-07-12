

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/templates")]
    [Authorize]
    public class CardTemplateController(
        ICardTemplateService cardTemplateService,
        ICurrentTenant currentTenant) : ControllerBase
    {
        private readonly ICardTemplateService _cardTemplateService = cardTemplateService ?? throw new ArgumentNullException(nameof(cardTemplateService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        [HttpGet]
        public async Task<IActionResult> GetActiveTemplates()
        {
            var result = await _cardTemplateService.GetActiveTemplatesAsync();
            return Ok(result);
        }
    }
}
