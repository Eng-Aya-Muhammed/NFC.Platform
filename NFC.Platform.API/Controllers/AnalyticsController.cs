using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize]
    public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService = analyticsService ?? throw new System.ArgumentNullException(nameof(analyticsService));

        [HttpGet("summary")]
        public async Task<IActionResult> GetUserAnalyticsSummary(CancellationToken cancellationToken)
        {
            var result = await _analyticsService.GetUserAnalyticsSummaryAsync(cancellationToken);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpGet("time-series")]
        public async Task<IActionResult> GetUserAnalyticsTimeSeries([FromQuery] string granularity = "monthly", CancellationToken cancellationToken = default)
        {
            var result = await _analyticsService.GetUserAnalyticsTimeSeriesAsync(granularity, cancellationToken);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpGet("leaderboard")]
        [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
        [HasPermission(AppPermissions.Analytics.View)]
        public async Task<IActionResult> GetCompanyLeaderboard(CancellationToken cancellationToken)
        {
            var result = await _analyticsService.GetCompanyLeaderboardAsync(cancellationToken);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpGet("employee/{employeeId:guid}")]
        [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
        [HasPermission(AppPermissions.Analytics.View)]
        public async Task<IActionResult> GetEmployeeDashboardAnalytics([FromRoute] Guid employeeId, CancellationToken cancellationToken = default)
        {
            var result = await _analyticsService.GetEmployeeDashboardAnalyticsAsync(employeeId, cancellationToken);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }
        [HttpGet("company/dashboard")]
        [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
        [HasPermission(AppPermissions.Analytics.View)]
        public async Task<IActionResult> GetCompanyDashboardAnalytics(CancellationToken cancellationToken = default)
        {
            var result = await _analyticsService.GetCompanyDashboardAnalyticsAsync(cancellationToken);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }
    }
}

