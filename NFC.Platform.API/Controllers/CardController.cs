using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;


namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/cards")]
    public class CardController(ICardService cardService, ICardOrderService cardOrderService) : ControllerBase
    {
        private readonly ICardService _cardService = cardService ?? throw new ArgumentNullException(nameof(cardService));
        private readonly ICardOrderService _cardOrderService = cardOrderService ?? throw new ArgumentNullException(nameof(cardOrderService));

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _cardService.GetByIdAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request)
        {
            var result = await _cardService.GetPagedCardsAsync(request);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Create([FromBody] CreateCardRequest request)
        {
            var result = await _cardService.CreateCardAsync(request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// User self-activates a card by providing its activation code.
        /// </summary>
        [HttpPost("activate")]
        [Authorize]
        [EnableRateLimiting("CardActivationPolicy")]
        public async Task<IActionResult> Activate([FromBody] ActivateCardRequest request)
        {
            var result = await _cardService.ActivateCardAsync(request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        // ── Encoding tool integration (admin only) ─────────────────────────────

        /// <summary>
        /// Returns cards for a given order optionally filtered by CardStatus.
        /// Used by the NFC encoding device to fetch unassigned codes.
        /// </summary>
        [HttpGet("for-encoding")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> GetCardsForEncoding([FromQuery] Guid orderId, [FromQuery] string? status = null)
        {
            var result = await _cardService.GetCardsForEncodingAsync(orderId, status);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Marks a single card as physically encoded. Auto-transitions the order to
        /// ReadyForDelivery when all cards in the order are encoded.
        /// </summary>
        [HttpPost("{id:guid}/mark-encoded")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> MarkEncoded([FromRoute] Guid id)
        {
            var result = await _cardService.MarkCardEncodedAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Admin-explicit single card activation.
        /// </summary>
        [HttpPost("{id:guid}/activate")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> ActivateById([FromRoute] Guid id)
        {
            var result = await _cardService.ActivateCardByIdAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Bulk-activates all cards belonging to a given order at delivery time.
        /// </summary>
        [HttpPost("activate-all-for-order")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> ActivateAllForOrder([FromQuery] Guid orderId)
        {
            var result = await _cardService.ActivateAllCardsForOrderAsync(orderId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Deactivates a card (lost/stolen/revoked). The public profile link stops resolving.
        /// </summary>
        [HttpPost("{id:guid}/deactivate")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Deactivate([FromRoute] Guid id)
        {
            var result = await _cardService.DeactivateCardAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _cardService.DeleteCardAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Reissues a lost/damaged card: deactivates the current card and places a new replacement order of quantity 1.
        /// </summary>
        [HttpPost("{id:guid}/reissue")]
        [Authorize]
        public async Task<IActionResult> Reissue([FromRoute] Guid id, [FromBody] ReissueCardRequest request)
        {
            var result = await _cardOrderService.ReissueCardAsync(id, request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }
    }
}
