namespace NFC.Platform.Application.Services;

    public class AnalyticsService(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ICurrentTenant currentTenant) : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        //  User: Summary 

        public async Task<ServiceResult<UserAnalyticsSummaryDto>> GetUserAnalyticsSummaryAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
                return ServiceResult<UserAnalyticsSummaryDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

            // Resolve the user's profile
            var profile = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value, cancellationToken);

            if (profile == null)
                return ServiceResult<UserAnalyticsSummaryDto>.NotFound(_messageService.Get("ProfileNotFound"));

            var metricRepo = _unitOfWork.Repository<ProfileMetric>();

            // Sequential aggregation queries
            var viewsCount = await metricRepo.CountAsync(m => m.UserProfileId == profile.Id && m.InteractionType == InteractionType.ProfileView, cancellationToken);
            var savesCount = await metricRepo.CountAsync(m => m.UserProfileId == profile.Id && m.InteractionType == InteractionType.ContactSaved, cancellationToken);
            var clicksCount = await metricRepo.CountAsync(m => m.UserProfileId == profile.Id && m.InteractionType == InteractionType.LinkClick, cancellationToken);


            // Last 6 months of views, broken down monthly
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthlyData = await metricRepo
                .GetQueryable()
                .AsNoTracking()
                .Where(m => m.UserProfileId == profile.Id
                         && m.InteractionType == InteractionType.ProfileView
                         && m.CreatedAt >= sixMonthsAgo)
                .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var monthlyViews = new List<MonthlyMetricDto>();
            for (var i = 5; i >= 0; i--)
            {
                var target = DateTime.UtcNow.AddMonths(-i);
                var match = monthlyData.FirstOrDefault(d => d.Year == target.Year && d.Month == target.Month);
                monthlyViews.Add(new MonthlyMetricDto
                {
                    MonthName = target.ToString("MMMM", System.Globalization.CultureInfo.CurrentUICulture),
                    Value = match?.Count ?? 0
                });
            }

            var dto = new UserAnalyticsSummaryDto
            {
                TotalProfileViews = viewsCount,
                TotalContactSaves = savesCount,
                TotalLinkClicks = clicksCount,

                MonthlyViews = monthlyViews
            };

            return ServiceResult<UserAnalyticsSummaryDto>.Success(dto);
        }

        //  User: Time-Series 

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

            // Fetch all metrics in the range in one query, then group in memory (small dataset)
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

        //  Company: Leaderboard 

        public async Task<ServiceResult<List<EmployeeLeaderboardEntryDto>>> GetCompanyLeaderboardAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<List<EmployeeLeaderboardEntryDto>>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

            // Fetch all employees for the company
            var employees = await _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId.Value && !e.IsDeleted)
                .ToListAsync(cancellationToken);

            if (employees.Count == 0)
                return ServiceResult<List<EmployeeLeaderboardEntryDto>>.Success([]);

            var employeeIds = employees.Select(e => e.Id).ToList();

            // Fetch all profiles linked to these employees in one query
            var profiles = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .AsNoTracking()
                .Where(p => p.EmployeeId.HasValue && employeeIds.Contains(p.EmployeeId.Value))
                .ToListAsync(cancellationToken);

            var profileIds = profiles.Select(p => p.Id).ToList();

            // Batch-fetch all relevant metrics in a single query
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

        //  Helpers 

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
