using Microsoft.AspNetCore.RateLimiting;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/public")]
    [AllowAnonymous]
    public class PublicProfileController(IProfileMetricService profileMetricService) : ControllerBase
    {
        private readonly IProfileMetricService _profileMetricService = profileMetricService ?? throw new ArgumentNullException(nameof(profileMetricService));

        /// <summary>
        /// Resolves a digital profile by its unique Id and returns the public profile data.
        /// </summary>
        [HttpGet("p/{id:guid}")]
        [HttpGet("profile/{id:guid}")]
        [EnableRateLimiting("ResolvePublicProfilePolicy")]
        public async Task<IActionResult> ResolvePublicProfile([FromRoute] Guid id)
        {
            var result = await _profileMetricService.ResolvePublicProfileAsync(id);
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
            var result = await _profileMetricService.RecordMetricAsync(profileId, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }
}
