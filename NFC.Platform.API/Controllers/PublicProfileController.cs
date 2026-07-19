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
        /// Resolves a physical card scan/tap by its activation code and retrieves the public profile.
        /// </summary>
        [HttpGet("cards/resolve/{activationCode}")]
        [EnableRateLimiting("ResolvePublicProfilePolicy")]
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
            var result = await _profileMetricService.RecordMetricAsync(profileId, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }
}
