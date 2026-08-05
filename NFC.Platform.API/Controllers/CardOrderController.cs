using NFC.Platform.BuildingBlocks.Common.Models;

namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/card-orders")]
public class CardOrderController(
    ICardOrderService cardOrderService,
    IMessageService messageService) : ControllerBase
{
    private readonly ICardOrderService _cardOrderService = cardOrderService ?? throw new ArgumentNullException(nameof(cardOrderService));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

    [HttpGet]
    [HasPermission(AppPermissions.CardOrders.View)]
    public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request, [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var result = await _cardOrderService.GetPagedOrdersAsync(request, status, search);
        return Ok(result);
    }

    [HttpGet("export/excel")]
    [HasPermission(AppPermissions.CardOrders.View)]
    public async Task<IActionResult> ExportExcel([FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var result = await _cardOrderService.ExportOrdersAsync(ExportFormat.Excel, status, search);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"CardOrders_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("export/pdf")]
    [HasPermission(AppPermissions.CardOrders.View)]
    public async Task<IActionResult> ExportPdf([FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var result = await _cardOrderService.ExportOrdersAsync(ExportFormat.Pdf, status, search);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

        var fileName = $"CardOrders_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf";
        return File(result.Data!, "application/pdf", fileName);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.CardOrders.View)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _cardOrderService.GetOrderByIdAsync(id);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.CardOrders.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCardOrderRequest request)
    {
        var result = await _cardOrderService.CreateOrderAsync(request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/reorder")]
    [HasPermission(AppPermissions.CardOrders.Create)]
    public async Task<IActionResult> Reorder([FromRoute] Guid id, [FromBody] ReorderRequest request)
    {
        var result = await _cardOrderService.CreateReorderAsync(id, request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.CardOrders.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCardOrderRequest request)
    {
        var result = await _cardOrderService.UpdateOrderAsync(id, request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPut("{id:guid}/cancel")]
    [HasPermission(AppPermissions.CardOrders.Cancel)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        var result = await _cardOrderService.CancelOrderAsync(id);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }


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
