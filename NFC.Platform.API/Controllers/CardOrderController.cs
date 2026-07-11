

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/card-orders")]
    [Authorize]
    public class CardOrderController(ICardOrderService cardOrderService) : ControllerBase
    {
        private readonly ICardOrderService _cardOrderService = cardOrderService ?? throw new ArgumentNullException(nameof(cardOrderService));

        /// <summary>
        /// Returns a paged list of card orders for the current tenant.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request)
        {
            var result = await _cardOrderService.GetPagedAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Returns a single card order with its items.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _cardOrderService.GetByIdAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Creates a new card order for the authenticated tenant user.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
        public async Task<IActionResult> Create([FromBody] CreateCardOrderRequest request)
        {
            var result = await _cardOrderService.CreateAsync(request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Updates the status of a card order (admin operation).
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromBody] UpdateCardOrderStatusRequest request)
        {
            var result = await _cardOrderService.UpdateStatusAsync(id, request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Soft-deletes a card order.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _cardOrderService.DeleteAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Assigns printed NFC card activation codes to order items (admin operation).
        /// </summary>
        [HttpPost("{id:guid}/assign-cards")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> AssignCards([FromRoute] Guid id, [FromBody] AssignCardsRequest request)
        {
            var result = await _cardOrderService.AssignCardsAsync(id, request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }
    }
}
