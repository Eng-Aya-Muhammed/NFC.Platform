namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class AdminController(IAdminService adminService, ICardService cardService) : ControllerBase
    {
        private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
        private readonly ICardService _cardService = cardService ?? throw new ArgumentNullException(nameof(cardService));

        /// <summary>
        /// Retrieves all orders across all tenants with optional status filtering and paging.
        /// </summary>
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrdersPaged([FromQuery] PaginationRequest request, [FromQuery] OrderStatus? status, [FromQuery(Name = "company_id")] Guid? companyId)
        {
            var result = await _adminService.GetOrdersPagedAsync(request, status, companyId);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a single order detailed view including custom design assets and customer properties.
        /// </summary>
        [HttpGet("orders/{id:guid}")]
        public async Task<IActionResult> GetOrderById([FromRoute] Guid id)
        {
            var result = await _adminService.GetOrderByIdAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Updates the status and tracking details of a physical card print order.
        /// </summary>
        [HttpPut("orders/{id:guid}/status")]
        public async Task<IActionResult> UpdateOrderStatus([FromRoute] Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            var result = await _adminService.UpdateOrderStatusAsync(id, dto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all custom template requests submitted by all users/companies.
        /// </summary>
        [HttpGet("template-requests")]
        [HttpGet("custom-design-requests")]
        public async Task<IActionResult> GetTemplateRequestsPaged([FromQuery] PaginationRequest request, [FromQuery] TemplateRequestStatus? status)
        {
            var result = await _adminService.GetTemplateRequestsPagedAsync(request, status);
            return Ok(result);
        }

        /// <summary>
        /// Approves or rejects custom template requests, optionally provisioning the custom template for the requesting tenant.
        /// </summary>
        [HttpPut("template-requests/{id:guid}/resolve")]
        public async Task<IActionResult> ResolveTemplateRequest([FromRoute] Guid id, [FromBody] ResolveTemplateRequestDto dto)
        {
            var result = await _adminService.ResolveTemplateRequestAsync(id, dto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Adds a new built-in template to the global system catalog.
        /// </summary>
        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateCardTemplateDto dto)
        {
            var result = await _adminService.CreateTemplateAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Modifies the styling parameters or layout configuration of an existing global card template.
        /// </summary>
        [HttpPut("templates/{id:guid}")]
        public async Task<IActionResult> UpdateTemplate([FromRoute] Guid id, [FromBody] UpdateCardTemplateDto dto)
        {
            var result = await _adminService.UpdateTemplateAsync(id, dto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Toggles the active status or deactivates a card template from the catalog.
        /// </summary>
        [HttpDelete("templates/{id:guid}")]
        public async Task<IActionResult> DeleteTemplate([FromRoute] Guid id)
        {
            var result = await _adminService.DeleteTemplateAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Lists all system tenants (individuals and companies) with active subscription tiers.
        /// </summary>
        [HttpGet("tenants")]
        public async Task<IActionResult> GetTenantsPaged([FromQuery] PaginationRequest request)
        {
            var result = await _adminService.GetTenantsPagedAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Suspends or reactivates a tenant account restricting resource accessibility.
        /// </summary>
        [HttpPut("tenants/{id:guid}/status")]
        public async Task<IActionResult> UpdateTenantStatus([FromRoute] Guid id, [FromBody] UpdateTenantStatusDto dto)
        {
            var result = await _adminService.UpdateTenantStatusAsync(id, dto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Retrieves cards for a given order, optionally filtered by status (e.g. unassigned_code, encoded).
        /// Used by the encoding tool integration.
        /// </summary>
        [HttpGet("cards")]
        public async Task<IActionResult> GetCards([FromQuery(Name = "order_id")] Guid orderId, [FromQuery] string? status = null)
        {
            var result = await _cardService.GetCardsForEncodingAsync(orderId, status);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
