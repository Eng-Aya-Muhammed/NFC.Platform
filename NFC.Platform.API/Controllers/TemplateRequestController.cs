using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/templates/requests")]
    [Authorize]
    public class TemplateRequestController(
        ITemplateRequestService templateRequestService,
        ICurrentTenant currentTenant) : ControllerBase
    {
        private readonly ITemplateRequestService _templateRequestService = templateRequestService ?? throw new ArgumentNullException(nameof(templateRequestService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        [HttpPost]
        [HasPermission(AppPermissions.TemplateRequests.Create)]
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

        [HttpPut("{id:guid}")]
        [HasPermission(AppPermissions.TemplateRequests.Update)]
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

        [HttpPatch("{id:guid}/cancel")]
        [HasPermission(AppPermissions.TemplateRequests.Cancel)]
        public async Task<IActionResult> CancelRequest([FromRoute] Guid id)
        {
            var result = await _templateRequestService.CancelRequestAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpGet]
        [HasPermission(AppPermissions.TemplateRequests.View)]
        public async Task<IActionResult> GetTenantRequests()
        {
            var result = await _templateRequestService.GetTenantRequestsAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [HasPermission(AppPermissions.TemplateRequests.View)]
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
