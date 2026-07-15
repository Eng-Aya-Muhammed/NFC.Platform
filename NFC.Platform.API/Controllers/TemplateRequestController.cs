

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Authorize]
    public class TemplateRequestController(
        ITemplateRequestService templateRequestService,
        ICurrentTenant currentTenant) : ControllerBase
    {
        private readonly ITemplateRequestService _templateRequestService = templateRequestService ?? throw new ArgumentNullException(nameof(templateRequestService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        [HttpPost("api/templates/requests")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateTemplateRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _templateRequestService.CreateRequestAsync(userId.Value, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("api/templates/requests")]
        public async Task<IActionResult> GetTenantRequests()
        {
            var result = await _templateRequestService.GetTenantRequestsAsync();
            return Ok(result);
        }

        [HttpGet("api/custom-design-requests/{id:guid}")]
        public async Task<IActionResult> GetRequestById([FromRoute] Guid id)
        {
            var result = await _templateRequestService.GetRequestByIdAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

    }
}
