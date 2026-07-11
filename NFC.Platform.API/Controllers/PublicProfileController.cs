using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/public")]
    [AllowAnonymous]
    public class PublicProfileController : ControllerBase
    {
        private readonly IProfileMetricService _profileMetricService;

        public PublicProfileController(IProfileMetricService profileMetricService)
        {
            _profileMetricService = profileMetricService ?? throw new ArgumentNullException(nameof(profileMetricService));
        }

        /// <summary>
        /// Resolves a physical card scan/tap by its activation code and retrieves the public profile.
        /// </summary>
        [HttpGet("cards/resolve/{activationCode}")]
        public async Task<IActionResult> ResolvePublicProfile([FromRoute] string activationCode)
        {
            var result = await _profileMetricService.ResolvePublicProfileAsync(activationCode);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Records an interaction metric (view, save contact, link click) for a user profile anonymously.
        /// </summary>
        [HttpPost("profiles/{profileId:guid}/metrics")]
        public async Task<IActionResult> RecordMetric([FromRoute] Guid profileId, [FromBody] RecordMetricRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _profileMetricService.RecordMetricAsync(profileId, request, ipAddress, userAgent);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }
}
