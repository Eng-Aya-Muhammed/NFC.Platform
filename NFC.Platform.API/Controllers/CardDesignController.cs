using NFC.Platform.BuildingBlocks.Common.Models;

namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/card-designs")]
public class CardDesignController(
    ICardDesignService cardDesignService,
    IMessageService messageService) : ControllerBase
{
    private readonly ICardDesignService _cardDesignService = cardDesignService ?? throw new ArgumentNullException(nameof(cardDesignService));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

    [HttpPost]
    [HasPermission(AppPermissions.CardDesigns.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCardDesignRequest request)
    {
        var result = await _cardDesignService.CreateDesignAsync(request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.CardDesigns.View)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _cardDesignService.GetDesignByIdAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet]
    [HasPermission(AppPermissions.CardDesigns.View)]
    public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request, [FromQuery] string? search = null)
    {
        var result = await _cardDesignService.GetPagedDesignsAsync(request, search);
        return Ok(result);
    }

    [HttpGet("{id:guid}/payment-url")]
    [HasPermission(AppPermissions.CardDesigns.View)]
    public async Task<IActionResult> GetPaymentUrl([FromRoute] Guid id)
    {
        var result = await _cardDesignService.GetPaymentUrlAsync(id);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{id:guid}/payment-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback(
        [FromRoute] Guid id,
        [FromBody] PaymentCallbackRequest request)
    {
        var result = await _cardDesignService.HandlePaymentCallbackAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
