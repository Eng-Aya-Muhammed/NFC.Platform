namespace NFC.Platform.Application.Services;

public class CompanyService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ICurrentTenant currentTenant) : ICompanyService
{
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public async Task<ServiceResult<CompanyProfileDto>> GetMyCompanyProfileAsync()
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<CompanyProfileDto>.Unauthorized(_messageService.Get("Unauthorized") ?? "User is not authenticated.");

            // Fetch the single company associated with the tenant
            var company = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .AsNoTracking()
                .Include(c => c.AdminUser)
                .FirstOrDefaultAsync();

            if (company == null)
                return ServiceResult<CompanyProfileDto>.NotFound(_messageService.Get("RecordNotFound"));

            var remainingDays = await GetSubscriptionRemainingDaysAsync(tenantId.Value);

            var companyDto = _mapper.Map<CompanyProfileDto>(company);
            companyDto.SubscriptionRemainingDays = remainingDays;

            return ServiceResult<CompanyProfileDto>.Success(companyDto);
        }

        public async Task<ServiceResult<CompanyProfileDto>> UpdateCompanyProfileAsync(UpdateCompanyProfileRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<CompanyProfileDto>.Unauthorized(_messageService.Get("Unauthorized") ?? "User is not authenticated.");

            var company = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .Include(c => c.AdminUser)
                .FirstOrDefaultAsync();

            if (company == null)
                return ServiceResult<CompanyProfileDto>.NotFound(_messageService.Get("RecordNotFound"));

            _mapper.Map(request, company);
            if (company.AdminUser != null)
            {
                company.AdminUser.PhoneNumber = request.Phone;
            }
            await _unitOfWork.SaveChangesAsync();

            var remainingDays = await GetSubscriptionRemainingDaysAsync(tenantId.Value);

            var companyDto = _mapper.Map<CompanyProfileDto>(company);
            companyDto.SubscriptionRemainingDays = remainingDays;

            return ServiceResult<CompanyProfileDto>.Success(companyDto, _messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult> ChangeCompanyAdminPasswordAsync(CompanyChangePasswordRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
                return ServiceResult.Unauthorized("User is not authenticated.");

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value);
            if (user == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            if (!PasswordHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
            {
                return ServiceResult.Fail(_messageService.Get("InvalidCredentials"), 400);
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("PasswordResetSuccess"));
        }

        private async Task<int> GetSubscriptionRemainingDaysAsync(Guid tenantId)
        {
            var subscription = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive && s.EndDate >= DateTime.UtcNow);

            if (subscription == null)
                return 0;

            var remaining = (subscription.EndDate - DateTime.UtcNow).Days;
            return remaining < 0 ? 0 : remaining;
        }

        public async Task<ServiceResult<CompanyDashboardDto>> GetCompanyDashboardAsync()
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<CompanyDashboardDto>.Unauthorized(_messageService.Get("Unauthorized") ?? "User is not authenticated.");

            // 1. Employee Count
            var employeesTask = _unitOfWork.Repository<Employee>().CountAsync();

            // 2. Card Orders Count
            var ordersTask = _unitOfWork.Repository<CardOrder>().CountAsync();

            // 3. Contact Saves Count
            var contactSavesTask = _unitOfWork.Repository<ProfileMetric>()
                .CountAsync(m => m.InteractionType == InteractionType.ContactSaved);

            await Task.WhenAll(employeesTask, ordersTask, contactSavesTask);

            var totalEmployees = employeesTask.Result;
            var cardRequests = ordersTask.Result;
            var contactSaves = contactSavesTask.Result;

            // 4. Top Employee (Most active profile, projection join)
            var topEmployeeName = await _unitOfWork.Repository<ProfileMetric>()
                .GetQueryable()
                .AsNoTracking()
                .GroupBy(m => new { m.UserProfileId, m.UserProfile.FullName })
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key.FullName)
                .FirstOrDefaultAsync() ?? "-";

            // 5. Monthly Metric statistics for the last 6 months (optimized database aggregation)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthlyData = await _unitOfWork.Repository<ProfileMetric>()
                .GetQueryable()
                .AsNoTracking()
                .Where(m => m.CreatedAt >= sixMonthsAgo)
                .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync();

            var monthlyStats = new List<MonthlyMetricDto>();

            for (var i = 5; i >= 0; i--)
            {
                var targetMonth = DateTime.UtcNow.AddMonths(-i);
                var match = monthlyData.FirstOrDefault(d => d.Year == targetMonth.Year && d.Month == targetMonth.Month);
                var count = match?.Count ?? 0;

                monthlyStats.Add(new MonthlyMetricDto
                {
                    MonthName = targetMonth.ToString("MMMM", System.Globalization.CultureInfo.CurrentUICulture),
                    Value = count
                });
            }

            var dashboardDto = new CompanyDashboardDto
            {
                ContactSavesCount = contactSaves,
                TotalEmployeesCount = totalEmployees,
                CardRequestsCount = cardRequests,
                TopEmployeeName = topEmployeeName,
                MonthlyMetrics = monthlyStats
            };

            return ServiceResult<CompanyDashboardDto>.Success(dashboardDto);
        }
    }
