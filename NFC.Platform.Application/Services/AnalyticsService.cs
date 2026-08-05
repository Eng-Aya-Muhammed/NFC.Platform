namespace NFC.Platform.Application.Services;

public class AnalyticsService(
    IUnitOfWork unitOfWork,
    IMessageService messageService,
    ICurrentTenant currentTenant) : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));


    public async Task<ServiceResult<UserAnalyticsSummaryDto>> GetUserAnalyticsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentTenant.UserId;
        if (!userId.HasValue)
            return ServiceResult<UserAnalyticsSummaryDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, cancellationToken);

        if (profile == null)
            return ServiceResult<UserAnalyticsSummaryDto>.NotFound(_messageService.Get("ProfileNotFound"));

        int remainingDays = 0;
        int totalSubDays = 365;
        var tenantId = _currentTenant.TenantId;
        var activeSubscription = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .AsNoTracking()
            .Where(s => (s.UserId == userId.Value || (tenantId.HasValue && s.TenantId == tenantId.Value)) && s.IsActive && !s.IsDeleted)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSubscription != null)
        {
            remainingDays = Math.Max(0, (activeSubscription.EndDate.Date - DateTime.UtcNow.Date).Days);
            totalSubDays = Math.Max(1, (activeSubscription.EndDate.Date - activeSubscription.StartDate.Date).Days);
        }

        var metricRepo = _unitOfWork.Repository<ProfileMetric>();
        var oneYearAgo = DateTime.UtcNow.AddMonths(-11);
        var startDate = new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var metricsList = await metricRepo.GetQueryable()
            .AsNoTracking()
            .Where(m => m.UserProfileId == profile.Id && m.CreatedAt >= startDate)
            .Select(m => new { m.InteractionType, m.CreatedAt })
            .ToListAsync(cancellationToken);

        var monthlyViewsCount = metricsList.Count(m => m.InteractionType == InteractionType.ProfileView && m.CreatedAt >= thirtyDaysAgo);
        var contactSavesCount = metricsList.Count(m => m.InteractionType == InteractionType.ContactSaved && m.CreatedAt >= thirtyDaysAgo);
        if (contactSavesCount == 0)
        {
            contactSavesCount = metricsList.Count(m => m.InteractionType == InteractionType.ContactSaved);
        }
        var clicksCount = metricsList.Count(m => m.InteractionType == InteractionType.LinkClick);

        var yearlyViewsTrend = new List<MonthlyViewsTrendDto>();
        var monthlyViewsLegacy = new List<MonthlyMetricDto>();
        var now = DateTime.UtcNow;

        for (int i = 11; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var viewsInMonth = metricsList.Count(m =>
                m.InteractionType == InteractionType.ProfileView &&
                m.CreatedAt.Year == monthDate.Year &&
                m.CreatedAt.Month == monthDate.Month);

            var monthName = monthDate.ToString("MMMM", System.Globalization.CultureInfo.CurrentUICulture);

            yearlyViewsTrend.Add(new MonthlyViewsTrendDto
            {
                Year = monthDate.Year,
                Month = monthDate.Month,
                MonthName = monthName,
                ViewsCount = viewsInMonth
            });

            if (i < 6)
            {
                monthlyViewsLegacy.Add(new MonthlyMetricDto
                {
                    MonthName = monthName,
                    Value = viewsInMonth
                });
            }
        }

        int totalYearlyViews = metricsList.Count(m => m.InteractionType == InteractionType.ProfileView);
        double saveRate = monthlyViewsCount > 0
            ? Math.Round(((double)contactSavesCount / monthlyViewsCount) * 100.0, 1)
            : (totalYearlyViews > 0 ? Math.Round(((double)contactSavesCount / totalYearlyViews) * 100.0, 1) : 0);

        var peakMonthItem = yearlyViewsTrend.OrderByDescending(x => x.ViewsCount).FirstOrDefault();
        var peakMonth = new PeakMonthDto
        {
            MonthName = peakMonthItem?.MonthName ?? string.Empty,
            ViewsCount = peakMonthItem?.ViewsCount ?? 0,
            FormattedText = peakMonthItem != null
                ? $"{peakMonthItem.MonthName} - {peakMonthItem.ViewsCount} {_messageService.Get("ViewsLabel")}"
                : string.Empty
        };

        double avgDailyViews = Math.Round((double)totalYearlyViews / 365.0, 1);

        var dto = new UserAnalyticsSummaryDto
        {
            TotalProfileViews = totalYearlyViews,
            TotalContactSaves = contactSavesCount,
            TotalLinkClicks = clicksCount,
            SubscriptionRemainingDays = remainingDays,
            TotalSubscriptionDays = totalSubDays,
            MonthlyViewsCount = monthlyViewsCount,
            ContactSavesCount = contactSavesCount,
            YearlyViewsTrend = yearlyViewsTrend,
            MonthlyViews = monthlyViewsLegacy,
            ContactSaveRate = saveRate,
            PeakMonth = peakMonth,
            TotalYearlyViews = totalYearlyViews,
            AverageDailyViews = avgDailyViews
        };

        return ServiceResult<UserAnalyticsSummaryDto>.Success(dto);
    }


    public async Task<ServiceResult<UserAnalyticsTimeSeriesDto>> GetUserAnalyticsTimeSeriesAsync(string granularity, CancellationToken cancellationToken = default)
    {
        var userId = _currentTenant.UserId;
        if (!userId.HasValue)
            return ServiceResult<UserAnalyticsTimeSeriesDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, cancellationToken);

        if (profile == null)
            return ServiceResult<UserAnalyticsTimeSeriesDto>.NotFound(_messageService.Get("ProfileNotFound"));

        bool isDaily = string.Equals(granularity, "daily", StringComparison.OrdinalIgnoreCase);
        var cutoff = isDaily ? DateTime.UtcNow.AddDays(-30) : DateTime.UtcNow.AddMonths(-6);

        var metrics = await _unitOfWork.Repository<ProfileMetric>()
            .GetQueryable()
            .AsNoTracking()
            .Where(m => m.UserProfileId == profile.Id && m.CreatedAt >= cutoff)
            .ToListAsync(cancellationToken);

        List<TimeSeriesDataPointDto> dataPoints;

        if (isDaily)
        {
            var lookup = metrics.GroupBy(m => m.CreatedAt.Date).ToDictionary(g => g.Key, g => g.ToList());
            dataPoints = Enumerable.Range(0, 30)
                .Select(daysAgo =>
                {
                    var day = DateTime.UtcNow.Date.AddDays(-29 + daysAgo);
                    lookup.TryGetValue(day, out var dayMetrics);
                    return BuildDataPoint(day.ToString("dd MMM"), dayMetrics);
                }).ToList();
        }
        else
        {
            var lookup = metrics.GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month }).ToDictionary(g => g.Key, g => g.ToList());
            dataPoints = Enumerable.Range(0, 6)
                .Select(monthsAgo =>
                {
                    var target = DateTime.UtcNow.AddMonths(-5 + monthsAgo);
                    var key = new { target.Year, target.Month };
                    lookup.TryGetValue(key, out var monthMetrics);
                    return BuildDataPoint(target.ToString("MMMM", System.Globalization.CultureInfo.CurrentUICulture), monthMetrics);
                }).ToList();
        }

        return ServiceResult<UserAnalyticsTimeSeriesDto>.Success(new UserAnalyticsTimeSeriesDto
        {
            Granularity = isDaily ? "daily" : "monthly",
            DataPoints = dataPoints
        });
    }


    public async Task<ServiceResult<List<EmployeeLeaderboardEntryDto>>> GetCompanyLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return ServiceResult<List<EmployeeLeaderboardEntryDto>>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

        var employees = await _unitOfWork.Repository<Employee>()
            .GetQueryable()
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId.Value && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
            return ServiceResult<List<EmployeeLeaderboardEntryDto>>.Success([]);

        var employeeIds = employees.Select(e => e.Id).ToList();

        var profiles = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .Where(p => p.EmployeeId.HasValue && employeeIds.Contains(p.EmployeeId.Value))
            .ToListAsync(cancellationToken);

        var profileIds = profiles.Select(p => p.Id).ToList();

        var metrics = profileIds.Count > 0
            ? await _unitOfWork.Repository<ProfileMetric>()
                .GetQueryable()
                .AsNoTracking()
                .Where(m => profileIds.Contains(m.UserProfileId))
                .ToListAsync(cancellationToken)
            : [];

        var metricsByProfile = metrics.GroupBy(m => m.UserProfileId).ToDictionary(g => g.Key, g => g.ToList());
        var profileByEmployeeId = profiles.Where(p => p.EmployeeId.HasValue)
            .ToDictionary(p => p.EmployeeId!.Value, p => p);

        var leaderboard = employees
            .Select(e =>
            {
                profileByEmployeeId.TryGetValue(e.Id, out var profile);
                var profileMetrics = profile != null && metricsByProfile.TryGetValue(profile.Id, out var m) ? m : [];
                var views = profileMetrics.Count(x => x.InteractionType == InteractionType.ProfileView);
                var saves = profileMetrics.Count(x => x.InteractionType == InteractionType.ContactSaved);
                var clicks = profileMetrics.Count(x => x.InteractionType == InteractionType.LinkClick);

                return new EmployeeLeaderboardEntryDto
                {
                    EmployeeId = e.Id,
                    FullName = e.FullName,
                    JobTitle = e.JobTitle,
                    Department = e.Department,
                    TotalViews = views,
                    TotalContactSaves = saves,
                    TotalLinkClicks = clicks,
                    TotalInteractions = views + saves + clicks
                };
            })
            .OrderByDescending(x => x.TotalInteractions)
            .Select((x, i) => { x.Rank = i + 1; return x; })
            .ToList();

        return ServiceResult<List<EmployeeLeaderboardEntryDto>>.Success(leaderboard);
    }

    public async Task<ServiceResult<EmployeeDashboardAnalyticsDto>> GetEmployeeDashboardAnalyticsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;

        var employee = await _unitOfWork.Repository<Employee>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
            return ServiceResult<EmployeeDashboardAnalyticsDto>.NotFound(_messageService.Get("EmployeeNotFound"));

        if (tenantId.HasValue && employee.TenantId != tenantId.Value)
            return ServiceResult<EmployeeDashboardAnalyticsDto>.Unauthorized(_messageService.Get("Unauthorized"));

        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EmployeeId == employee.Id, cancellationToken);

        if (profile == null)
            return ServiceResult<EmployeeDashboardAnalyticsDto>.NotFound(_messageService.Get("ProfileNotFound"));

        int remainingDays = 0;
        int totalSubDays = 365;
        if (tenantId.HasValue)
        {
            var activeSubscription = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId.Value && s.IsActive && !s.IsDeleted)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeSubscription != null)
            {
                remainingDays = Math.Max(0, (activeSubscription.EndDate.Date - DateTime.UtcNow.Date).Days);
                totalSubDays = Math.Max(1, (activeSubscription.EndDate.Date - activeSubscription.StartDate.Date).Days);
            }
        }

        var metricRepo = _unitOfWork.Repository<ProfileMetric>();
        var oneYearAgo = DateTime.UtcNow.AddMonths(-11);
        var startDate = new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var metricsList = await metricRepo.GetQueryable()
            .AsNoTracking()
            .Where(m => m.UserProfileId == profile.Id && m.CreatedAt >= startDate)
            .Select(m => new { m.InteractionType, m.CreatedAt })
            .ToListAsync(cancellationToken);

        var monthlyViews = metricsList.Count(m => m.InteractionType == InteractionType.ProfileView && m.CreatedAt >= thirtyDaysAgo);
        var contactSavesCount = metricsList.Count(m => m.InteractionType == InteractionType.ContactSaved && m.CreatedAt >= thirtyDaysAgo);
        if (contactSavesCount == 0)
        {
            contactSavesCount = metricsList.Count(m => m.InteractionType == InteractionType.ContactSaved);
        }

        var yearlyViewsTrend = new List<MonthlyViewsTrendDto>();
        var now = DateTime.UtcNow;
        for (int i = 11; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var viewsInMonth = metricsList.Count(m =>
                m.InteractionType == InteractionType.ProfileView &&
                m.CreatedAt.Year == monthDate.Year &&
                m.CreatedAt.Month == monthDate.Month);

            yearlyViewsTrend.Add(new MonthlyViewsTrendDto
            {
                Year = monthDate.Year,
                Month = monthDate.Month,
                MonthName = monthDate.ToString("MMMM", System.Globalization.CultureInfo.CurrentUICulture),
                ViewsCount = viewsInMonth
            });
        }

        int totalYearlyViews = metricsList.Count(m => m.InteractionType == InteractionType.ProfileView);
        double saveRate = monthlyViews > 0
            ? Math.Round(((double)contactSavesCount / monthlyViews) * 100.0, 1)
            : (totalYearlyViews > 0 ? Math.Round(((double)contactSavesCount / totalYearlyViews) * 100.0, 1) : 0);

        var peakMonthItem = yearlyViewsTrend.OrderByDescending(x => x.ViewsCount).FirstOrDefault();
        var peakMonth = new PeakMonthDto
        {
            MonthName = peakMonthItem?.MonthName ?? string.Empty,
            ViewsCount = peakMonthItem?.ViewsCount ?? 0,
            FormattedText = peakMonthItem != null
                ? $"{peakMonthItem.MonthName} - {peakMonthItem.ViewsCount} {_messageService.Get("ViewsLabel")}"
                : string.Empty
        };

        double avgDailyViews = Math.Round((double)totalYearlyViews / 365.0, 1);

        var result = new EmployeeDashboardAnalyticsDto
        {
            SubscriptionRemainingDays = remainingDays,
            TotalSubscriptionDays = totalSubDays,
            MonthlyViews = monthlyViews,
            ContactSavesCount = contactSavesCount,
            YearlyViewsTrend = yearlyViewsTrend,
            ContactSaveRate = saveRate,
            PeakMonth = peakMonth,
            TotalYearlyViews = totalYearlyViews,
            AverageDailyViews = avgDailyViews
        };

        return ServiceResult<EmployeeDashboardAnalyticsDto>.Success(result);
    }
    public async Task<ServiceResult<CompanyDashboardAnalyticsDto>> GetCompanyDashboardAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return ServiceResult<CompanyDashboardAnalyticsDto>.Unauthorized(_messageService.Get("TenantRequired"));

        var totalEmployees = await _unitOfWork.Repository<Employee>()
            .GetQueryable()
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId.Value && !e.IsDeleted)
            .CountAsync(cancellationToken);

        var companyProfiles = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.Employee != null && p.Employee.TenantId == tenantId.Value && !p.Employee.IsDeleted)
            .ToListAsync(cancellationToken);

        var profileIds = companyProfiles.Select(p => p.Id).ToList();

        var metricsRepo = _unitOfWork.Repository<ProfileMetric>();
        var oneYearAgo = DateTime.UtcNow.AddMonths(-11);
        var startDate = new DateTime(oneYearAgo.Year, oneYearAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var metricsList = await metricsRepo.GetQueryable()
            .AsNoTracking()
            .Where(m => profileIds.Contains(m.UserProfileId) && m.CreatedAt >= startDate)
            .Select(m => new { m.InteractionType, m.CreatedAt, m.UserProfileId })
            .ToListAsync(cancellationToken);

        var totalContactSaves = await metricsRepo.GetQueryable()
            .AsNoTracking()
            .Where(m => profileIds.Contains(m.UserProfileId) && m.InteractionType == InteractionType.ContactSaved)
            .CountAsync(cancellationToken);

        EmployeeLeaderboardEntryDto? mostVisited = null;
        if (metricsList.Count != 0)
        {
            var topProfileId = metricsList
                .Where(m => m.InteractionType == InteractionType.ProfileView)
                .GroupBy(m => m.UserProfileId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            var topProfile = companyProfiles.FirstOrDefault(p => p.Id == topProfileId);
            if (topProfile != null && topProfile.Employee != null)
            {
                mostVisited = new EmployeeLeaderboardEntryDto
                {
                    EmployeeId = topProfile.Employee.Id,
                    FullName = topProfile.FullName,
                    JobTitle = topProfile.JobTitle,
                    Department = topProfile.Department,
                    ProfilePictureUrl = topProfile.ProfilePictureUrl,
                    TotalViews = metricsList.Count(m => m.InteractionType == InteractionType.ProfileView && m.UserProfileId == topProfile.Id)
                };
            }
        }

        var yearlyViewsTrend = new List<MonthlyViewsTrendDto>();
        var now = DateTime.UtcNow;

        for (int i = 11; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var viewsInMonth = metricsList.Count(m =>
                m.InteractionType == InteractionType.ProfileView &&
                m.CreatedAt.Year == monthDate.Year &&
                m.CreatedAt.Month == monthDate.Month);

            var monthName = monthDate.ToString("MMMM", System.Globalization.CultureInfo.CurrentUICulture);

            yearlyViewsTrend.Add(new MonthlyViewsTrendDto
            {
                Year = monthDate.Year,
                Month = monthDate.Month,
                MonthName = monthName,
                ViewsCount = viewsInMonth
            });
        }

        var result = new CompanyDashboardAnalyticsDto
        {
            TotalEmployees = totalEmployees,
            TotalContactSaves = totalContactSaves,
            MostVisitedEmployee = mostVisited,
            TimeSeriesData = yearlyViewsTrend
        };

        return ServiceResult<CompanyDashboardAnalyticsDto>.Success(result);
    }


    private static TimeSeriesDataPointDto BuildDataPoint(string label, List<ProfileMetric>? metrics)
    {
        if (metrics == null || metrics.Count == 0)
            return new TimeSeriesDataPointDto { Label = label };

        return new TimeSeriesDataPointDto
        {
            Label = label,
            Views = metrics.Count(m => m.InteractionType == InteractionType.ProfileView),
            ContactSaves = metrics.Count(m => m.InteractionType == InteractionType.ContactSaved),
            LinkClicks = metrics.Count(m => m.InteractionType == InteractionType.LinkClick)
        };
    }
}
