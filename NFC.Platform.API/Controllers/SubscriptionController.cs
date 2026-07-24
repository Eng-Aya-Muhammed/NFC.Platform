using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/subscription")]
    public class SubscriptionController(ISubscriptionService subscriptionService) : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            var result = await _subscriptionService.GetPlansAsync();
            return Ok(result);
        }

        [HttpGet("current")]
        [HasPermission(AppPermissions.Company.View)]
        public async Task<IActionResult> GetCurrent()
        {
            var result = await _subscriptionService.GetCurrentSubscriptionAsync();
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpGet("history")]
        [HasPermission(AppPermissions.Company.View)]
        public async Task<IActionResult> GetHistory()
        {
            var result = await _subscriptionService.GetSubscriptionHistoryAsync();
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPost("subscribe")]
        [HasPermission(AppPermissions.Company.Update)]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            var result = await _subscriptionService.SubscribeAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPost("renew")]
        [HasPermission(AppPermissions.Company.Update)]
        public async Task<IActionResult> Renew([FromBody] RenewSubscriptionRequest request)
        {
            var result = await _subscriptionService.RenewSubscriptionAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
