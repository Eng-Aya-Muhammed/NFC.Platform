using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize]
    public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService = analyticsService ?? throw new System.ArgumentNullException(nameof(analyticsService));

        /// <summary>
        /// Returns aggregated profile interaction totals for the authenticated user.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetUserAnalyticsSummary()
        {
            var result = await _analyticsService.GetUserAnalyticsSummaryAsync();
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Returns time-series interaction data for the authenticated user's profile.
        /// Query param: granularity = "daily" (last 30 days) or "monthly" (last 6 months, default).
        /// </summary>
        [HttpGet("time-series")]
        public async Task<IActionResult> GetUserAnalyticsTimeSeries([FromQuery] string granularity = "monthly")
        {
            var result = await _analyticsService.GetUserAnalyticsTimeSeriesAsync(granularity);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        /// <summary>
        /// Returns the company employee analytics leaderboard, ranked by total interactions.
        /// Accessible to company admins only.
        /// </summary>
        [HttpGet("/api/company/analytics/leaderboard")]
        [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
        public async Task<IActionResult> GetCompanyLeaderboard()
        {
            var result = await _analyticsService.GetCompanyLeaderboardAsync();
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }
    }
}
