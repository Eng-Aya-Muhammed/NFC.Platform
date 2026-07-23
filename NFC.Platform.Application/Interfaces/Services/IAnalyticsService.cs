using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.Analytics;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    /// <summary>
    /// Service contract for aggregating profile interaction metrics into
    /// user-facing and company-facing analytics dashboards.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Returns aggregated interaction totals for the currently authenticated user's profile.
        /// </summary>
        Task<ServiceResult<UserAnalyticsSummaryDto>> GetUserAnalyticsSummaryAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns time-series interaction data for chart rendering.
        /// Granularity: "daily" (last 30 days) or "monthly" (last 6 months).
        /// </summary>
        Task<ServiceResult<UserAnalyticsTimeSeriesDto>> GetUserAnalyticsTimeSeriesAsync(string granularity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns an employee leaderboard ranked by total profile interactions
        /// across the current company tenant.
        /// </summary>
        Task<ServiceResult<List<EmployeeLeaderboardEntryDto>>> GetCompanyLeaderboardAsync(CancellationToken cancellationToken = default);
    }
}

