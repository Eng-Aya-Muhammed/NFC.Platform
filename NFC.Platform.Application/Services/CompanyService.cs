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
                return ServiceResult<CompanyProfileDto>.Unauthorized(_messageService.Get("Unauthorized"));

            // Fetch the single company associated with the tenant
            var company = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .AsNoTracking()
                .Include(c => c.AdminUser)
                    .ThenInclude(u => u.UserProfile)
                        .ThenInclude(p => p!.CustomLinks)
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
                return ServiceResult<CompanyProfileDto>.Unauthorized(_messageService.Get("Unauthorized"));

            var company = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .Include(c => c.AdminUser)
                    .ThenInclude(u => u.UserProfile)
                        .ThenInclude(p => p!.CustomLinks)
                .FirstOrDefaultAsync();

            if (company == null)
                return ServiceResult<CompanyProfileDto>.NotFound(_messageService.Get("RecordNotFound"));

            _mapper.Map(request, company);
            if (company.AdminUser != null)
            {
                if (!string.IsNullOrWhiteSpace(request.Phone))
                    company.AdminUser.PhoneNumber = request.Phone;

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    company.AdminUser.Email = request.Email;
                    if (company.AdminUser.UserProfile != null)
                        company.AdminUser.UserProfile.ContactEmail = request.Email;
                }

                if (request.Links?.Count > 0)
                {
                    if (company.AdminUser.UserProfile == null)
                    {
                        company.AdminUser.UserProfile = new UserProfile { UserId = company.AdminUserId, TenantId = tenantId.Value };
                        await _unitOfWork.Repository<UserProfile>().AddAsync(company.AdminUser.UserProfile);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    company.AdminUser.UserProfile.UpdateCustomLinks(request.Links);
                }
            }
            await _unitOfWork.SaveChangesAsync();

            var remainingDays = await GetSubscriptionRemainingDaysAsync(tenantId.Value);

            var companyDto = _mapper.Map<CompanyProfileDto>(company);
            companyDto.SubscriptionRemainingDays = remainingDays;

            return ServiceResult<CompanyProfileDto>.Success(companyDto, _messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult<CompanyProfileDto>> UpdateCompanyTemplateAsync(Guid? templateId)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<CompanyProfileDto>.Unauthorized(_messageService.Get("Unauthorized"));

            var company = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .Include(c => c.AdminUser)
                .FirstOrDefaultAsync();

            if (company == null)
                return ServiceResult<CompanyProfileDto>.NotFound(_messageService.Get("RecordNotFound"));

            if (!templateId.HasValue)
            {
                company.ProfileTemplateId = null;
            }
            else
            {
                // 1. Verify the requested template exists and is active
                var template = await _unitOfWork.Repository<CardTemplate>()
                    .GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive && !t.IsDeleted);

                if (template == null)
                    return ServiceResult<CompanyProfileDto>.NotFound(_messageService.Get("RecordNotFound"));

                // 2. Load active subscription (with plan templates for access check)
                var activeSub = await NFC.Platform.Application.Extensions.SubscriptionHelper.GetActiveSubWithTemplatesAsync(_unitOfWork, tenantId.Value);

                if (activeSub == null)
                    return ServiceResult<CompanyProfileDto>.Fail(_messageService.Get("SubscriptionExpiredOrMissing"), 400);

                // 3. Check template is assigned to this plan
                var isTemplateAllowed = activeSub.SubscriptionPlan.PlanTemplates
                    .Any(pt => pt.CardTemplateId == templateId);

                if (!isTemplateAllowed)
                    return ServiceResult<CompanyProfileDto>.Fail(_messageService.Get("TemplateNotAllowedInPlan"), 403);

                // 4. Check template-change limit
                var limit = activeSub.SubscriptionPlan.MaxTemplateChanges;
                if (limit != NFC.Platform.Domain.Constants.SubscriptionConstants.UnlimitedQuota && activeSub.TemplateChangesUsed >= limit)
                    return ServiceResult<CompanyProfileDto>.Fail(_messageService.Get("TemplateChangeLimitReached"), 400);

                // 5. Apply change + increment counter
                company.ProfileTemplateId = templateId;
                activeSub.TemplateChangesUsed++;
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
                return ServiceResult.Unauthorized(_messageService.Get("Unauthorized"));

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
                return ServiceResult<CompanyDashboardDto>.Unauthorized(_messageService.Get("Unauthorized"));

            // 1. Employee Count
            var totalEmployees = await _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .AsNoTracking()
                .CountAsync(e => e.TenantId == tenantId.Value && !e.IsDeleted);

            // 2. Card Orders Count
            var cardRequests = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .CountAsync(o => o.TenantId == tenantId.Value && !o.IsDeleted);

            // 3. Contact Saves Count
            var contactSaves = await _unitOfWork.Repository<ProfileMetric>()
                .GetQueryable()
                .AsNoTracking()
                .CountAsync(m => m.TenantId == tenantId.Value && m.InteractionType == InteractionType.ContactSaved);

            // 4. Top Employee Details
            DTOs.Analytics.TopEmployeeDto? topEmployee = null;
            var topMetricGroup = await _unitOfWork.Repository<ProfileMetric>()
                .GetQueryable()
                .AsNoTracking()
                .Where(m => m.TenantId == tenantId.Value)
                .GroupBy(m => new { m.UserProfileId, m.UserProfile.FullName, m.UserProfile.EmployeeId, m.UserProfile.ProfilePictureUrl })
                .OrderByDescending(g => g.Count())
                .Select(g => new
                {
                    g.Key.UserProfileId,
                    g.Key.FullName,
                    g.Key.EmployeeId,
                    g.Key.ProfilePictureUrl,
                    ViewsCount = g.Count(x => x.InteractionType == InteractionType.ProfileView),
                    SavesCount = g.Count(x => x.InteractionType == InteractionType.ContactSaved)
                })
                .FirstOrDefaultAsync();

            var topName = topMetricGroup?.FullName ?? "-";

            if (topMetricGroup != null && topMetricGroup.EmployeeId.HasValue)
            {
                var emp = await _unitOfWork.Repository<Employee>()
                    .GetByIdAsync(topMetricGroup.EmployeeId.Value);

                topEmployee = new DTOs.Analytics.TopEmployeeDto
                {
                    EmployeeId = topMetricGroup.EmployeeId.Value,
                    FullName = topMetricGroup.FullName,
                    JobTitle = emp?.JobTitle ?? string.Empty,
                    Department = emp?.Department ?? string.Empty,
                    ProfilePictureUrl = topMetricGroup.ProfilePictureUrl,
                    TotalViews = topMetricGroup.ViewsCount,
                    TotalContactSaves = topMetricGroup.SavesCount
                };
            }

            // 5. Monthly Metric statistics for the last 12 months (yearly trend)
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-11);
            var startDate = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var monthlyData = await _unitOfWork.Repository<ProfileMetric>()
                .GetQueryable()
                .AsNoTracking()
                .Where(m => m.TenantId == tenantId.Value && m.CreatedAt >= startDate)
                .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync();

            var monthlyStats = new List<MonthlyMetricDto>();

            for (var i = 11; i >= 0; i--)
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
                TopEmployeeName = topName,
                TopPerformingEmployee = topEmployee,
                MonthlyMetrics = monthlyStats
            };

            return ServiceResult<CompanyDashboardDto>.Success(dashboardDto);
        }

        public async Task<ServiceResult<DTOs.VipCustomer.VipCustomerDto>> UpdateVipStatusAsync(Guid companyId, DTOs.VipCustomer.UpdateVipStatusRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var company = await _unitOfWork.Repository<Company>()
                .GetByIdAsync(companyId);

            if (company == null)
                return ServiceResult<DTOs.VipCustomer.VipCustomerDto>.NotFound(_messageService.Get("RecordNotFound"));

            company.IsVip = request.IsVip;
            company.VipDisplayOrder = request.VipDisplayOrder;

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<DTOs.VipCustomer.VipCustomerDto>.Success(_mapper.Map<DTOs.VipCustomer.VipCustomerDto>(company), _messageService.Get("RecordUpdated"));
        }
    }

