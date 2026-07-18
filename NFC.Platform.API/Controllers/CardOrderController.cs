using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.API.Models;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/card-orders")]
    [Authorize]
    public class CardOrderController(ICardOrderService cardOrderService, IMessageService messageService) : ControllerBase
    {
        private readonly ICardOrderService _cardOrderService = cardOrderService ?? throw new ArgumentNullException(nameof(cardOrderService));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        /// <summary>
        /// Returns the unit and total price for a given card type and quantity.
        /// Used to preview cost before placing an order.
        /// </summary>
        [HttpGet("pricing")]
        [HttpGet("/api/order-draft/pricing")]
        public async Task<IActionResult> GetPricing([FromQuery] string cardType = "plastic", [FromQuery] int quantity = 1)
        {
            var result = await _cardOrderService.GetOrderPricingAsync(cardType, quantity);
            return Ok(result);
        }

        /// <summary>
        /// Returns all currently active card pricing configurations.
        /// </summary>
        [HttpGet("pricing-catalog")]
        [HttpGet("/api/pricing/config")]
        public async Task<IActionResult> GetActivePricingCatalog()
        {
            var result = await _cardOrderService.GetActivePricingCatalogAsync();
            return Ok(result);
        }

        /// <summary>
        /// Returns a paged list of card orders for the current tenant.
        /// Optional query param: status (e.g. PendingReview, InPrinting).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request, [FromQuery] string? status = null)
        {
            var result = await _cardOrderService.GetPagedAsync(request, status);
            return Ok(result);
        }

        /// <summary>
        /// Returns a single card order with its items.
        /// </summary>
        [HttpGet("{id:guid}")]
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
        public async Task<IActionResult> Create([FromBody] CreateCardOrderRequest request)
        {
            var result = await _cardOrderService.CreateAsync(request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Creates a reorder: new order that reuses the design/template from the parent order.
        /// </summary>
        [HttpPost("{id:guid}/reorder")]
        public async Task<IActionResult> Reorder([FromRoute] Guid id, [FromBody] ReorderRequest request)
        {
            var result = await _cardOrderService.CreateReorderAsync(id, request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return StatusCode(result.StatusCode, result);
        }


        /// <summary>
        /// Soft-deletes a card order. Only allowed while Status = PendingReview.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _cardOrderService.DeleteAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }


        /// <summary>
        /// Retrieves the Excel ingestion status for a bulk order.
        /// </summary>
        [HttpGet("/api/orders/{id:guid}/employees-import-status")]
        public async Task<IActionResult> GetEmployeesImportStatus([FromRoute] Guid id)
        {
            var result = await _cardOrderService.GetEmployeesImportStatusAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Handles bulk card ordering and Excel directory import for employees.
        /// </summary>
        [HttpPost("~/api/company/orders/bulk")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PlaceBulkOrderFromExcel([FromForm] ImportEmployeesAndOrderCardsRequest request)
        {
            if (request == null || request.File == null)
            {
                return BadRequest(_messageService.Get("NoFileUploaded") ?? "No file was uploaded.");
            }

            var result = await _cardOrderService.QueueEmployeeImportJobAsync(
                request.File,
                request.CardType,
                request.CardDesignType,
                request.Notes,
                request.DesignReferenceUrl,
                request.LogoUrl,
                request.DesignNotes);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return StatusCode(202, result); // 202 Accepted
        }
    }
}
