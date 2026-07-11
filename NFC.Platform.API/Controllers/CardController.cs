

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/cards")]
    public class CardController(ICardService cardService) : ControllerBase
    {
        private readonly ICardService _cardService = cardService ?? throw new ArgumentNullException(nameof(cardService));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _cardService.GetByIdAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request)
        {
            var result = await _cardService.GetPagedCardsAsync(request);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCardRequest request)
        {
            var result = await _cardService.CreateCardAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivateCardRequest request)
        {
            var result = await _cardService.ActivateCardAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _cardService.DeleteCardAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }
}
