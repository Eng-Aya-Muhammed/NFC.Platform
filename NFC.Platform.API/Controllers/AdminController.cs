namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class AdminController(
    IAdminService adminService,
    ICardTemplateService cardTemplateService,
    ITemplateCategoryService templateCategoryService,
    ICardTypeService cardTypeService,
    ICardPackageService cardPackageService,
    IDiscountCodeService discountCodeService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
    private readonly ICardTemplateService _cardTemplateService = cardTemplateService ?? throw new ArgumentNullException(nameof(cardTemplateService));
    private readonly ITemplateCategoryService _templateCategoryService = templateCategoryService ?? throw new ArgumentNullException(nameof(templateCategoryService));
    private readonly ICardTypeService _cardTypeService = cardTypeService ?? throw new ArgumentNullException(nameof(cardTypeService));
    private readonly ICardPackageService _cardPackageService = cardPackageService ?? throw new ArgumentNullException(nameof(cardPackageService));
    private readonly IDiscountCodeService _discountCodeService = discountCodeService ?? throw new ArgumentNullException(nameof(discountCodeService));

    // ==========================================
    // Order Management
    // ==========================================

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrdersPaged(
        [FromQuery] PaginationRequest request,
        [FromQuery] OrderStatus? status,
        [FromQuery(Name = "company_id")] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetOrdersPagedAsync(request, status, companyId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id)
    {
        var result = await _adminService.GetOrderByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("orders/{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus([FromRoute] Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var result = await _adminService.UpdateOrderStatusAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("orders/{id:guid}/verify-otp")]
    public async Task<IActionResult> VerifyDeliveryOtp([FromRoute] Guid id, [FromBody] VerifyDeliveryOtpRequest request)
    {
        var result = await _adminService.VerifyDeliveryOtpAsync(id, request.Otp);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("orders/{id:guid}/resend-otp")]
    public async Task<IActionResult> ResendDeliveryOtp([FromRoute] Guid id)
    {
        var result = await _adminService.ResendDeliveryOtpAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ==========================================
    // Custom Template Requests
    // ==========================================

    [HttpGet("template-requests")]
    [HttpGet("custom-design-requests")]
    public async Task<IActionResult> GetTemplateRequestsPaged([FromQuery] PaginationRequest request, [FromQuery] TemplateRequestStatus? status, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetTemplateRequestsPagedAsync(request, status, cancellationToken);
        return Ok(result);
    }

    [HttpPut("template-requests/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveTemplateRequest([FromRoute] Guid id, [FromBody] ResolveTemplateRequestDto dto)
    {
        var result = await _adminService.ResolveTemplateRequestAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ==========================================
    // Card Templates Management
    // ==========================================

    [HttpGet("card-templates")]
    [HasPermission(AppPermissions.Templates.View)]
    public async Task<IActionResult> GetAllAdminCardTemplates([FromQuery] PaginationRequest request)
    {
        var result = await _cardTemplateService.GetAllAdminTemplatesAsync(request);
        return Ok(result);
    }

    [HttpGet("card-templates/{id:guid}")]
    [HasPermission(AppPermissions.Templates.View)]
    public async Task<IActionResult> GetCardTemplateById([FromRoute] Guid id)
    {
        var result = await _cardTemplateService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("card-templates")]
    [HttpPost("templates")]
    [HasPermission(AppPermissions.Templates.Create)]
    public async Task<IActionResult> CreateCardTemplate([FromBody] CreateCardTemplateRequest request)
    {
        var result = await _cardTemplateService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("card-templates/{id:guid}")]
    [HttpPut("templates/{id:guid}")]
    [HasPermission(AppPermissions.Templates.Update)]
    public async Task<IActionResult> UpdateCardTemplate([FromRoute] Guid id, [FromBody] UpdateCardTemplateRequest request)
    {
        var result = await _cardTemplateService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("card-templates/{id:guid}")]
    [HttpDelete("templates/{id:guid}")]
    [HasPermission(AppPermissions.Templates.Delete)]
    public async Task<IActionResult> DeleteCardTemplate([FromRoute] Guid id)
    {
        var result = await _cardTemplateService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ==========================================
    // Template Categories Management
    // ==========================================

    [HttpGet("template-categories")]
    [HasPermission(AppPermissions.TemplateCategories.View)]
    public async Task<IActionResult> GetAllAdminTemplateCategories([FromQuery] PaginationRequest request)
    {
        var result = await _templateCategoryService.GetAllAdminCategoriesAsync(request);
        return Ok(result);
    }

    [HttpGet("template-categories/{id:guid}")]
    [HasPermission(AppPermissions.TemplateCategories.View)]
    public async Task<IActionResult> GetTemplateCategoryById([FromRoute] Guid id)
    {
        var result = await _templateCategoryService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("template-categories")]
    [HasPermission(AppPermissions.TemplateCategories.Create)]
    public async Task<IActionResult> CreateTemplateCategory([FromBody] CreateTemplateCategoryRequest request)
    {
        var result = await _templateCategoryService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("template-categories/{id:guid}")]
    [HasPermission(AppPermissions.TemplateCategories.Update)]
    public async Task<IActionResult> UpdateTemplateCategory([FromRoute] Guid id, [FromBody] UpdateTemplateCategoryRequest request)
    {
        var result = await _templateCategoryService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("template-categories/{id:guid}")]
    [HasPermission(AppPermissions.TemplateCategories.Delete)]
    public async Task<IActionResult> DeleteTemplateCategory([FromRoute] Guid id)
    {
        var result = await _templateCategoryService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ==========================================
    // Card Types Management
    // ==========================================

    [HttpGet("card-types")]
    [HasPermission(AppPermissions.CardTypes.View)]
    public async Task<IActionResult> GetAllAdminCardTypes([FromQuery] PaginationRequest request)
    {
        var result = await _cardTypeService.GetAllAdminCardTypesAsync(request);
        return Ok(result);
    }

    [HttpGet("card-types/{id:guid}")]
    [HasPermission(AppPermissions.CardTypes.View)]
    public async Task<IActionResult> GetCardTypeById([FromRoute] Guid id)
    {
        var result = await _cardTypeService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("card-types")]
    [HasPermission(AppPermissions.CardTypes.Create)]
    public async Task<IActionResult> CreateCardType([FromBody] CreateCardTypeRequest request)
    {
        var result = await _cardTypeService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("card-types/{id:guid}")]
    [HasPermission(AppPermissions.CardTypes.Update)]
    public async Task<IActionResult> UpdateCardType([FromRoute] Guid id, [FromBody] UpdateCardTypeRequest request)
    {
        var result = await _cardTypeService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("card-types/{id:guid}")]
    [HasPermission(AppPermissions.CardTypes.Delete)]
    public async Task<IActionResult> DeleteCardType([FromRoute] Guid id)
    {
        var result = await _cardTypeService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ==========================================
    // Card Packages Management
    // ==========================================

    [HttpGet("card-packages")]
    [HasPermission(AppPermissions.CardPackages.View)]
    public async Task<IActionResult> GetAllAdminCardPackages([FromQuery] PaginationRequest request)
    {
        var result = await _cardPackageService.GetAllAdminCardPackagesAsync(request);
        return Ok(result);
    }

    [HttpGet("card-packages/{id:guid}")]
    [HasPermission(AppPermissions.CardPackages.View)]
    public async Task<IActionResult> GetCardPackageById([FromRoute] Guid id)
    {
        var result = await _cardPackageService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("card-packages")]
    [HasPermission(AppPermissions.CardPackages.Create)]
    public async Task<IActionResult> CreateCardPackage([FromBody] CreateCardPackageRequest request)
    {
        var result = await _cardPackageService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("card-packages/{id:guid}")]
    [HasPermission(AppPermissions.CardPackages.Update)]
    public async Task<IActionResult> UpdateCardPackage([FromRoute] Guid id, [FromBody] UpdateCardPackageRequest request)
    {
        var result = await _cardPackageService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("card-packages/{id:guid}")]
    [HasPermission(AppPermissions.CardPackages.Delete)]
    public async Task<IActionResult> DeleteCardPackage([FromRoute] Guid id)
    {
        var result = await _cardPackageService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ==========================================
    // Tenant Management
    // ==========================================

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenantsPaged([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetTenantsPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("tenants/{id:guid}/status")]
    public async Task<IActionResult> UpdateTenantStatus([FromRoute] Guid id, [FromBody] UpdateTenantStatusDto dto)
    {
        var result = await _adminService.UpdateTenantStatusAsync(id, dto);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ==========================================
    // Subscription Plan Management
    // ==========================================

    [HttpGet("subscription-plans")]
    [HttpGet("plans")]
    public async Task<IActionResult> GetAllAdminPlans([FromQuery] PaginationRequest request)
    {
        var result = await _adminService.GetAllAdminPlansAsync(request);
        return Ok(result);
    }

    [HttpGet("subscription-plans/{id:guid}")]
    [HttpGet("plans/{id:guid}")]
    public async Task<IActionResult> GetPlanById([FromRoute] Guid id)
    {
        var result = await _adminService.GetPlanByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("subscription-plans")]
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequest request)
    {
        var result = await _adminService.CreatePlanAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("subscription-plans/{planId:guid}")]
    [HttpPut("plans/{planId:guid}")]
    public async Task<IActionResult> UpdatePlan([FromRoute] Guid planId, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        var result = await _adminService.UpdatePlanAsync(planId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("subscription-plans/{planId:guid}")]
    [HttpDelete("plans/{planId:guid}")]
    public async Task<IActionResult> DeletePlan([FromRoute] Guid planId)
    {
        var result = await _adminService.DeletePlanAsync(planId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("plans/{planId:guid}/templates")]
    public async Task<IActionResult> GetPlanTemplates([FromRoute] Guid planId)
    {
        var result = await _adminService.GetPlanTemplatesAsync(planId);
        return Ok(result);
    }

    [HttpPost("plans/{planId:guid}/templates/{templateId:guid}")]
    public async Task<IActionResult> AssignTemplate([FromRoute] Guid planId, [FromRoute] Guid templateId)
    {
        var result = await _adminService.AssignTemplateAsync(planId, templateId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("plans/{planId:guid}/templates/{templateId:guid}")]
    public async Task<IActionResult> UnassignTemplate([FromRoute] Guid planId, [FromRoute] Guid templateId)
    {
        var result = await _adminService.UnassignTemplateAsync(planId, templateId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    // ==========================================
    // Discount Code Management
    // ==========================================

    [HttpGet("discount-codes")]
    [HasPermission(AppPermissions.DiscountCodes.View)]
    public async Task<IActionResult> GetDiscountCodesPaged([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await _discountCodeService.GetPagedAdminAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("discount-codes/{id:guid}")]
    [HasPermission(AppPermissions.DiscountCodes.View)]
    public async Task<IActionResult> GetDiscountCodeById([FromRoute] Guid id)
    {
        var result = await _discountCodeService.GetByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("discount-codes")]
    [HasPermission(AppPermissions.DiscountCodes.Create)]
    public async Task<IActionResult> CreateDiscountCode([FromBody] CreateDiscountCodeRequest request)
    {
        var result = await _discountCodeService.CreateAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("discount-codes/{id:guid}")]
    [HasPermission(AppPermissions.DiscountCodes.Update)]
    public async Task<IActionResult> UpdateDiscountCode([FromRoute] Guid id, [FromBody] UpdateDiscountCodeRequest request)
    {
        var result = await _discountCodeService.UpdateAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("discount-codes/{id:guid}")]
    [HasPermission(AppPermissions.DiscountCodes.Delete)]
    public async Task<IActionResult> DeleteDiscountCode([FromRoute] Guid id)
    {
        var result = await _discountCodeService.DeleteAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
