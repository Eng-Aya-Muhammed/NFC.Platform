using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs.Analytics;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IAnalyticsService
    {
        Task<ServiceResult<UserAnalyticsSummaryDto>> GetUserAnalyticsSummaryAsync(CancellationToken cancellationToken = default);

        Task<ServiceResult<UserAnalyticsTimeSeriesDto>> GetUserAnalyticsTimeSeriesAsync(string granularity, CancellationToken cancellationToken = default);

        Task<ServiceResult<List<EmployeeLeaderboardEntryDto>>> GetCompanyLeaderboardAsync(CancellationToken cancellationToken = default);

        Task<ServiceResult<EmployeeDashboardAnalyticsDto>> GetEmployeeDashboardAnalyticsAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<ServiceResult<CompanyDashboardAnalyticsDto>> GetCompanyDashboardAnalyticsAsync(CancellationToken cancellationToken = default);
    }
}

