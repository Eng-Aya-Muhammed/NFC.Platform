using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Authorize]
    public class TemplateRequestController : ControllerBase
    {
        private readonly ITemplateRequestService _templateRequestService;
        private readonly ICurrentTenant _currentTenant;

        public TemplateRequestController(
            ITemplateRequestService templateRequestService,
            ICurrentTenant currentTenant)
        {
            _templateRequestService = templateRequestService ?? throw new ArgumentNullException(nameof(templateRequestService));
            _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        }

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

        [HttpPatch("api/admin/templates/requests/{id:guid}/status")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateRequestStatus([FromRoute] Guid id, [FromBody] TemplateRequestStatus status)
        {
            var result = await _templateRequestService.UpdateRequestStatusAsync(id, status);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
