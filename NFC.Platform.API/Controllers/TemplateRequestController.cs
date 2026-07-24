using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    public class TemplateRequestController(
        ITemplateRequestService templateRequestService,
        ICurrentTenant currentTenant) : ControllerBase
    {
        private readonly ITemplateRequestService _templateRequestService = templateRequestService ?? throw new ArgumentNullException(nameof(templateRequestService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        [HttpPost("api/templates/requests")]
        [HasPermission(AppPermissions.Templates.Create)]
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

        [HttpPut("api/templates/requests/{id:guid}")]
        [HasPermission(AppPermissions.Templates.Update)]
        public async Task<IActionResult> UpdateRequest([FromRoute] Guid id, [FromBody] UpdateTemplateRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _templateRequestService.UpdateRequestAsync(id, userId.Value, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPatch("api/templates/requests/{id:guid}/cancel")]
        [HasPermission(AppPermissions.Templates.Cancel)]
        public async Task<IActionResult> CancelRequest([FromRoute] Guid id)
        {
            var result = await _templateRequestService.CancelRequestAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpGet("api/templates/requests")]
        [HasPermission(AppPermissions.Templates.View)]
        public async Task<IActionResult> GetTenantRequests()
        {
            var result = await _templateRequestService.GetTenantRequestsAsync();
            return Ok(result);
        }

        [HttpGet("api/custom-design-requests/{id:guid}")]
        [HasPermission(AppPermissions.Templates.View)]
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
