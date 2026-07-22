namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

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
        /// Verifies the delivery OTP for a ReadyForDelivery order.
        /// On success, marks the order as Delivered and clears the OTP.
        /// </summary>
        [HttpPost("orders/{id:guid}/verify-otp")]
        public async Task<IActionResult> VerifyDeliveryOtp([FromRoute] Guid id, [FromBody] VerifyDeliveryOtpRequest request)
        {
            var result = await _adminService.VerifyDeliveryOtpAsync(id, request.Otp);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Resends the delivery OTP for a ReadyForDelivery order.
        /// Generates a new 6-digit OTP, updates expiry (+7 days), enforces a 60-second cooldown, and re-triggers Email & WhatsApp notifications.
        /// </summary>
        [HttpPost("orders/{id:guid}/resend-otp")]
        public async Task<IActionResult> ResendDeliveryOtp([FromRoute] Guid id)
        {
            var result = await _adminService.ResendDeliveryOtpAsync(id);
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
        /// Updates global pricing structure for a card material.
        /// </summary>
        [HttpPost("pricing")]
        public async Task<IActionResult> UpdateCardPricing([FromBody] UpdateCardPricingDto dto)
        {
            var result = await _adminService.UpdateCardPricingAsync(dto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }



        // ── Subdomain management ──────────────────────────────────────────────────

        /// <summary>
        /// Lists all profile subdomains across all tenants with optional search by name or subdomain.
        /// Used for oversight and conflict detection.
        /// </summary>
        [HttpGet("subdomains")]
        public async Task<IActionResult> GetAllSubdomains([FromQuery] PaginationRequest request, [FromQuery] string? search)
        {
            var result = await _adminService.GetAllProfileSubdomainsAsync(request, search);
            return Ok(result);
        }

        /// <summary>
        /// Forcibly reassigns a subdomain for any profile (conflict resolution / policy violation).
        /// </summary>
        [HttpPut("subdomains/{profileId:guid}")]
        public async Task<IActionResult> ReassignSubdomain([FromRoute] Guid profileId, [FromBody] ReassignSubdomainDto dto)
        {
            var result = await _adminService.ReassignSubdomainAsync(profileId, dto.Subdomain);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        // ── Subscription Plan Management ──────────────────────────────────────────

        /// <summary>Creates a new subscription plan with optional initial template assignments.</summary>
        [HttpPost("plans")]
        public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequest request)
        {
            var result = await _adminService.CreatePlanAsync(request);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>Updates an existing subscription plan (patch semantics — only provided fields are applied).</summary>
        [HttpPut("plans/{planId:guid}")]
        public async Task<IActionResult> UpdatePlan([FromRoute] Guid planId, [FromBody] UpdateSubscriptionPlanRequest request)
        {
            var result = await _adminService.UpdatePlanAsync(planId, request);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>Soft-deletes a subscription plan. Blocked if any tenant has an active subscription on this plan.</summary>
        [HttpDelete("plans/{planId:guid}")]
        public async Task<IActionResult> DeletePlan([FromRoute] Guid planId)
        {
            var result = await _adminService.DeletePlanAsync(planId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        // ── Plan Template Assignment ───────────────────────────────────────────────

        /// <summary>Returns all templates currently assigned to a plan.</summary>
        [HttpGet("plans/{planId:guid}/templates")]
        public async Task<IActionResult> GetPlanTemplates([FromRoute] Guid planId)
        {
            var result = await _adminService.GetPlanTemplatesAsync(planId);
            return Ok(result);
        }

        /// <summary>Assigns a template to a subscription plan.</summary>
        [HttpPost("plans/{planId:guid}/templates/{templateId:guid}")]
        public async Task<IActionResult> AssignTemplate([FromRoute] Guid planId, [FromRoute] Guid templateId)
        {
            var result = await _adminService.AssignTemplateAsync(planId, templateId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>Removes a template assignment from a subscription plan. Does not affect users currently using the template.</summary>
        [HttpDelete("plans/{planId:guid}/templates/{templateId:guid}")]
        public async Task<IActionResult> UnassignTemplate([FromRoute] Guid planId, [FromRoute] Guid templateId)
        {
            var result = await _adminService.UnassignTemplateAsync(planId, templateId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }
    }
}
