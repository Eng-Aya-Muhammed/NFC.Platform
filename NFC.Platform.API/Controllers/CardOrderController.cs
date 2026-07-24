using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Enums;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;


using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;


namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/card-orders")]
    public class CardOrderController(
        ICardOrderService cardOrderService, 
        ICardPricingService cardPricingService,
        IEmployeeImportService employeeImportService,
        IMessageService messageService) : ControllerBase
    {
        private readonly ICardOrderService _cardOrderService = cardOrderService ?? throw new ArgumentNullException(nameof(cardOrderService));
        private readonly ICardPricingService _cardPricingService = cardPricingService ?? throw new ArgumentNullException(nameof(cardPricingService));
        private readonly IEmployeeImportService _employeeImportService = employeeImportService ?? throw new ArgumentNullException(nameof(employeeImportService));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        /// <summary>
        /// Returns the unit and total price for a given card type and quantity.
        /// Used to preview cost before placing an order.
        /// </summary>
        [HttpGet("pricing")]
        [HttpGet("/api/order-draft/pricing")]
        [HasPermission(AppPermissions.CardOrders.Create)]
        public async Task<IActionResult> GetPricing([FromQuery] CardType cardType = CardType.Plastic, [FromQuery] int quantity = 1)
        {
            var result = await _cardPricingService.CalculateOrderPricingAsync(cardType, quantity);
            return Ok(result);
        }

        /// <summary>
        /// Returns all currently active card pricing configurations.
        /// </summary>
        [HttpGet("pricing-catalog")]
        [HttpGet("/api/pricing/config")]
        [HasPermission(AppPermissions.CardOrders.Create)]
        public async Task<IActionResult> GetActivePricingCatalog()
        {
            var result = await _cardPricingService.GetActiveCatalogAsync();
            return Ok(result);
        }

        /// <summary>
        /// Returns a paged list of card orders for the current tenant.
        /// Optional query param: status (e.g. PendingReview, InPrinting).
        /// </summary>
        [HttpGet]
        [HasPermission(AppPermissions.CardOrders.View)]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request, [FromQuery] string? status = null)
        {
            var result = await _cardOrderService.GetPagedOrdersAsync(request, status);
            return Ok(result);
        }

        /// <summary>
        /// Returns a single card order with its items.
        /// </summary>
        [HttpGet("{id:guid}")]
        [HasPermission(AppPermissions.CardOrders.View)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _cardOrderService.GetOrderByIdAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Creates a new card order for the authenticated tenant user.
        /// </summary>
        [HttpPost]
        [HasPermission(AppPermissions.CardOrders.Create)]
        public async Task<IActionResult> Create([FromBody] CreateCardOrderRequest request)
        {
            var result = await _cardOrderService.CreateOrderAsync(request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Creates a reorder: new order that reuses the design/template from the parent order.
        /// </summary>
        [HttpPost("{id:guid}/reorder")]
        [HasPermission(AppPermissions.CardOrders.Create)]
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
        [HasPermission(AppPermissions.CardOrders.Update)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _cardOrderService.DeleteOrderAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }


        /// <summary>
        /// Retrieves the Excel ingestion status for a bulk order.
        /// </summary>
        [HttpGet("/api/orders/{id:guid}/employees-import-status")]
        [HasPermission(AppPermissions.CardOrders.View)]
        public async Task<IActionResult> GetEmployeesImportStatus([FromRoute] Guid id)
        {
            var result = await _employeeImportService.GetImportStatusAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }


        /// <summary>
        /// Resends the delivery OTP for an order belonging to the current tenant.
        /// Generates a new 6-digit OTP, updates expiry (+7 days), enforces a 60-second cooldown, and re-triggers Email & WhatsApp notifications.
        /// </summary>
        [HttpPost("{id:guid}/resend-otp")]
        [HasPermission(AppPermissions.CardOrders.Update)]
        public async Task<IActionResult> ResendDeliveryOtp([FromRoute] Guid id)
        {
            var result = await _cardOrderService.ResendOrderOtpAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
