

namespace NFC.Platform.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly IStorageService _storageService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public AdminService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        IStorageService storageService,
        IBackgroundJobClient backgroundJobClient)
    {
        _unitOfWork           = unitOfWork           ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper               = mapper               ?? throw new ArgumentNullException(nameof(mapper));
        _messageService       = messageService       ?? throw new ArgumentNullException(nameof(messageService));
        _storageService       = storageService       ?? throw new ArgumentNullException(nameof(storageService));
        _backgroundJobClient  = backgroundJobClient  ?? throw new ArgumentNullException(nameof(backgroundJobClient));
    }

    public async Task<ServiceResult<PagedResult<AdminOrderSummaryDto>>> GetOrdersPagedAsync(PaginationRequest request, OrderStatus? statusFilter, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Tenant)
                .ThenInclude(t => t.Company)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(o => o.Status == statusFilter.Value);
        }

        if (companyId.HasValue)
        {
            query = query.Where(o => o.Tenant.Company != null && o.Tenant.Company.Id == companyId.Value);
        }

        var pagedResult = await query.ToPagedResultAsync(request, o => _mapper.Map<AdminOrderSummaryDto>(o), cancellationToken);
        return ServiceResult<PagedResult<AdminOrderSummaryDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<AdminOrderDetailDto>> GetOrderByIdAsync(Guid id)
    {
        var order = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Tenant)
            .Include(o => o.User)
                .ThenInclude(u => u.UserProfile)
            .Include(o => o.CardType)
            .Include(o => o.CardPackage)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return ServiceResult<AdminOrderDetailDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<AdminOrderDetailDto>(order);

        // Populate SelectedTemplate if user profile has a template
        if (order.User?.UserProfile?.ProfileTemplateId != null)
        {
            var template = await _unitOfWork.Repository<CardTemplate>()
                .GetByIdAsync(order.User.UserProfile.ProfileTemplateId.Value);
            if (template != null)
            {
                dto.SelectedTemplate = _mapper.Map<CardTemplateAdminDto>(template);
            }
        }

        // If no SelectedTemplate exists, fetch the user's latest TemplateRequest
        if (dto.SelectedTemplate == null)
        {
            var latestRequest = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Where(r => r.RequestedByUserId == order.UserId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestRequest != null)
            {
                dto.LatestTemplateRequest = _mapper.Map<TemplateRequestDto>(latestRequest);
            }
        }

        return ServiceResult<AdminOrderDetailDto>.Success(dto);
    }

    public async Task<ServiceResult> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto)
    {
        var orderRepo = _unitOfWork.Repository<CardOrder>();
        var order = await orderRepo.GetQueryable()
            .Include(o => o.Items)
            .Include(o => o.Tenant)
                .ThenInclude(t => t.Company)
                    .ThenInclude(c => c!.AdminUser)
                        .ThenInclude(u => u!.UserProfile)
            .Include(o => o.User)
                .ThenInclude(u => u.UserProfile)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (!IsValidStatusTransition(order.Status, dto.Status))
            return ServiceResult.Fail(
                _messageService.Get("InvalidStatusTransition", order.Status.ToString(), dto.Status.ToString()), 422);

        if (dto.Status == OrderStatus.Rejected)
        {
            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                return ServiceResult.Fail(_messageService.Get("RejectionReasonRequired"), 422);

            order.RejectionReason = dto.RejectionReason;
        }

        if (dto.Status == OrderStatus.ReadyForDelivery && order.DeliveryMethod == DeliveryMethod.Courier)
        {
            if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
                return ServiceResult.Fail(_messageService.Get("TrackingNumberRequired"), 422);
        }

        if (!string.IsNullOrWhiteSpace(dto.TrackingNumber))
            order.TrackingNumber = dto.TrackingNumber;

        order.Status = dto.Status;

        if (dto.Status == OrderStatus.ReadyForDelivery)
        {
            var recipient = order.Tenant?.Company?.AdminUser ?? order.User;
            if (recipient != null)
            {
                var otp = GenerateOtp();
                order.DeliveryOtp = otp;
                order.DeliveryOtpExpiresAt = DateTime.UtcNow.AddDays(7);
                order.DeliveryOtpLastSentAt = DateTime.UtcNow;
                order.DeliveryOtpResendCount = 0;

                EnqueueOtpNotifications(recipient, otp, order.CardName, isResend: false);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult> VerifyDeliveryOtpAsync(Guid orderId, string otp)
    {
        var order = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.ReadyForDelivery)
            return ServiceResult.Fail(_messageService.Get("OrderNotReadyForDelivery"), 422);

        if (order.DeliveryOtpExpiresAt.HasValue && order.DeliveryOtpExpiresAt.Value < DateTime.UtcNow)
            return ServiceResult.Fail(_messageService.Get("OtpExpired"), 422);

        if (order.DeliveryOtp != otp)
            return ServiceResult.Fail(_messageService.Get("InvalidOtp"), 422);

        order.Status = OrderStatus.Delivered;
        order.DeliveryOtp = null;
        order.DeliveryOtpExpiresAt = null;
        order.DeliveryOtpLastSentAt = null;
        order.DeliveryOtpResendCount = 0;
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("OrderDelivered"));
    }

    public async Task<ServiceResult> ResendDeliveryOtpAsync(Guid orderId)
    {
        var orderRepo = _unitOfWork.Repository<CardOrder>();
        var order = await orderRepo.GetQueryable()
            .Include(o => o.Tenant)
                .ThenInclude(t => t.Company)
                    .ThenInclude(c => c!.AdminUser)
                        .ThenInclude(u => u!.UserProfile)
            .Include(o => o.User)
                .ThenInclude(u => u.UserProfile)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.ReadyForDelivery)
            return ServiceResult.Fail(_messageService.Get("OrderNotReadyForDelivery"), 422);

        if (order.DeliveryOtpLastSentAt.HasValue &&
            (DateTime.UtcNow - order.DeliveryOtpLastSentAt.Value).TotalSeconds < 60)
        {
            return ServiceResult.Fail(_messageService.Get("OtpCooldownActive"), 422);
        }

        if (order.DeliveryOtpResendCount >= 5)
        {
            return ServiceResult.Fail(_messageService.Get("OtpResendLimitReached"), 422);
        }

        var recipient = order.Tenant?.Company?.AdminUser ?? order.User;

        var newOtp = GenerateOtp();
        order.DeliveryOtp = newOtp;
        order.DeliveryOtpExpiresAt = DateTime.UtcNow.AddDays(7);
        order.DeliveryOtpLastSentAt = DateTime.UtcNow;
        order.DeliveryOtpResendCount++;

        await _unitOfWork.SaveChangesAsync();

        if (recipient != null)
        {
            EnqueueOtpNotifications(recipient, newOtp, order.CardName, isResend: true);
        }

        return ServiceResult.Success(_messageService.Get("OtpResent"));
    }

    public async Task<ServiceResult<PagedResult<TemplateRequestDto>>> GetTemplateRequestsPagedAsync(
        PaginationRequest request, TemplateRequestStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<TemplateRequest>()
            .GetQueryable()
            .AsNoTracking()
            .Include(r => r.Tenant)
            .Include(r => r.RequestedByUser)
            .OrderByDescending(r => r.CreatedAt)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var pagedResult = await query.ToPagedResultAsync(request, r => _mapper.Map<TemplateRequestDto>(r), cancellationToken);
        return ServiceResult<PagedResult<TemplateRequestDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult> ResolveTemplateRequestAsync(Guid id, ResolveTemplateRequestDto dto)
    {
        var requestRepo = _unitOfWork.Repository<TemplateRequest>();
        var templateRequest = await requestRepo.GetQueryable()
            .Include(r => r.Tenant)
                .ThenInclude(t => t.Company)
                    .ThenInclude(c => c!.AdminUser)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (templateRequest == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            templateRequest.Status = dto.Status;
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                templateRequest.Notes = dto.Notes.StartsWith("Admin Notes:", StringComparison.OrdinalIgnoreCase) ? dto.Notes : $"Admin Notes: {dto.Notes}";
            }

            if (dto.Status == TemplateRequestStatus.Completed)
            {
                var customCategory = _unitOfWork.Repository<TemplateCategory>()
                    .GetQueryable()
                    .FirstOrDefault(c => c.NameEn == "Custom");

                if (customCategory == null)
                {
                    customCategory = new TemplateCategory
                    {
                        NameAr = "مخصص",
                        NameEn = "Custom",
                        IsActive = true,
                        DisplayOrder = 99
                    };
                    await _unitOfWork.Repository<TemplateCategory>().AddAsync(customCategory);
                }

                var customTemplate = new CardTemplate
                {
                    NameAr = templateRequest.TemplateName,
                    NameEn = templateRequest.TemplateName,
                    CategoryId = customCategory.Id,
                    PhotoUrl = templateRequest.ReferenceImageUrl ?? templateRequest.LogoUrl ?? "",
                    IsActive = true,
                    DisplayOrder = 1
                };

                await _unitOfWork.Repository<CardTemplate>().AddAsync(customTemplate);
                templateRequest.ProducedTemplateId = customTemplate.Id;

                var company = await _unitOfWork.Repository<Company>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(c => c.TenantId == templateRequest.TenantId);

                if (company != null)
                {
                    company.ProfileTemplateId = customTemplate.Id;
                }
                else
                {
                    var userProfile = await _unitOfWork.Repository<UserProfile>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(p => p.TenantId == templateRequest.TenantId && p.UserId == templateRequest.RequestedByUserId);

                    if (userProfile != null)
                    {
                        userProfile.ProfileTemplateId = customTemplate.Id;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            if (dto.Status == TemplateRequestStatus.Completed)
            {
                var recipientEmail = templateRequest.Tenant?.Company?.AdminUser?.Email ?? templateRequest.RequestedByUser?.Email;
                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                    if (string.IsNullOrWhiteSpace(culture)) culture = "ar";

                    _backgroundJobClient.Enqueue<IEmailService>(x =>
                        x.SendTemplateRequestApprovedEmailAsync(recipientEmail, templateRequest.TemplateName, culture));
                }
            }
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<CardTemplateAdminDto>> CreateTemplateAsync(CreateCardTemplateRequest dto)
    {
        var template = _mapper.Map<CardTemplate>(dto);

        await _unitOfWork.Repository<CardTemplate>().AddAsync(template);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<CardTemplateAdminDto>(template);
        return ServiceResult<CardTemplateAdminDto>.Success(resultDto);
    }

    public async Task<ServiceResult<CardTemplateAdminDto>> UpdateTemplateAsync(Guid id, UpdateCardTemplateRequest dto)
    {
        var templateRepo = _unitOfWork.Repository<CardTemplate>();
        var template = await templateRepo.GetByIdAsync(id);

        if (template == null)
            return ServiceResult<CardTemplateAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        _mapper.Map(dto, template);
        _unitOfWork.Repository<CardTemplate>().Update(template);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<CardTemplateAdminDto>(template);
        return ServiceResult<CardTemplateAdminDto>.Success(resultDto);
    }

    public async Task<ServiceResult> DeleteTemplateAsync(Guid id)
    {
        var templateRepo = _unitOfWork.Repository<CardTemplate>();
        var template = await templateRepo.GetByIdAsync(id);

        if (template == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            template.IsDeleted = true;
            template.IsActive = false;

            var affectedProfiles = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .Where(p => p.ProfileTemplateId == id)
                .ToListAsync();

            foreach (var profile in affectedProfiles)
                profile.ProfileTemplateId = null;

            var affectedCompanies = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .Where(c => c.ProfileTemplateId == id)
                .ToListAsync();

            foreach (var company in affectedCompanies)
                company.ProfileTemplateId = null;

            var planAssignments = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
                .GetQueryable()
                .Where(pt => pt.CardTemplateId == id)
                .ToListAsync();

            foreach (var assignment in planAssignments)
                _unitOfWork.Repository<SubscriptionPlanTemplate>().Remove(assignment);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return ServiceResult.Success(_messageService.Get("TemplateDeletedAndProfilesCleared"));
    }

    public async Task<ServiceResult<PagedResult<TenantSummaryDto>>> GetTenantsPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Tenant>()
            .GetQueryable()
            .AsNoTracking()
            .Include(t => t.Company)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        var pagedTenants = await query.ToPagedResultAsync(request, t => t, cancellationToken);
        var tenantIds = pagedTenants.Items.Select(t => t.Id).ToList();

        var activeSubscriptions = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .AsNoTracking()
            .Include(us => us.SubscriptionPlan)
            .Where(us => tenantIds.Contains(us.TenantId) && us.IsActive)
            .ToListAsync(cancellationToken);

        var activeSubByTenant = activeSubscriptions
            .GroupBy(us => us.TenantId)
            .ToDictionary(
                g => g.Key, 
                g => g.OrderByDescending(us => us.EndDate).FirstOrDefault()
            );

        var tenantSummaryDtos = new List<TenantSummaryDto>();
        foreach (var tenant in pagedTenants.Items)
        {
            var dto = _mapper.Map<TenantSummaryDto>(tenant);
            dto.AccountType = tenant.Company != null ? "Company" : "Individual";

            if (activeSubByTenant.TryGetValue(tenant.Id, out var activeSub) && activeSub != null)
            {
                dto.ActivePlanName = activeSub.SubscriptionPlan != null
                    ? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? activeSub.SubscriptionPlan.NameAr : activeSub.SubscriptionPlan.NameEn)
                    : null;
                dto.SubscriptionExpiry = activeSub.EndDate;
                dto.DaysRemaining = Math.Max(0, (int)(activeSub.EndDate - DateTime.UtcNow).TotalDays);
            }
            else
            {
                dto.ActivePlanName = "Free / No Active Plan";
                dto.DaysRemaining = 0;
            }

            tenantSummaryDtos.Add(dto);
        }

        var result = PagedResult<TenantSummaryDto>.Create(
            tenantSummaryDtos,
            pagedTenants.TotalCount,
            pagedTenants.PageNumber,
            pagedTenants.PageSize
        );

        return ServiceResult<PagedResult<TenantSummaryDto>>.Success(result);
    }

    public async Task<ServiceResult> UpdateTenantStatusAsync(Guid id, UpdateTenantStatusDto dto)
    {
        var tenantRepo = _unitOfWork.Repository<Tenant>();
        var tenant = await tenantRepo.GetByIdAsync(id);

        if (tenant == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        tenant.IsActive = dto.IsActive;
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<PagedResult<SubscriptionPlanAdminDto>>> GetAllAdminPlansAsync(
        PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<SubscriptionPlan>()
            .GetQueryable()
            .AsNoTracking()
            .Include(p => p.PlanTemplates)
                .ThenInclude(pt => pt.CardTemplate)
            .OrderByDescending(p => p.CreatedAt);

        var pagedResult = await query.ToPagedResultAsync(request, p => _mapper.Map<SubscriptionPlanAdminDto>(p), cancellationToken);
        return ServiceResult<PagedResult<SubscriptionPlanAdminDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<SubscriptionPlanAdminDto>> GetPlanByIdAsync(Guid id)
    {
        var plan = await _unitOfWork.Repository<SubscriptionPlan>()
            .GetQueryable()
            .AsNoTracking()
            .Include(p => p.PlanTemplates)
                .ThenInclude(pt => pt.CardTemplate)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
            return ServiceResult<SubscriptionPlanAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<SubscriptionPlanAdminDto>(plan);
        return ServiceResult<SubscriptionPlanAdminDto>.Success(dto);
    }

    public async Task<ServiceResult<SubscriptionPlanAdminDto>> CreatePlanAsync(CreateSubscriptionPlanRequest request)
    {
        var trimmedAr = request.NameAr?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedAr))
        {
            var nameArExists = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AnyAsync(p => p.NameAr.Trim() == trimmedAr);
            if (nameArExists)
                return ServiceResult<SubscriptionPlanAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        var trimmedEn = request.NameEn?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedEn))
        {
            var nameEnExists = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AnyAsync(p => p.NameEn.Trim() == trimmedEn);
            if (nameEnExists)
                return ServiceResult<SubscriptionPlanAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

        var plan = _mapper.Map<SubscriptionPlan>(request);
        await _unitOfWork.Repository<SubscriptionPlan>().AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        if (request.TemplateIds?.Count > 0)
        {
            foreach (var templateId in request.TemplateIds)
            {
                var template = await _unitOfWork.Repository<CardTemplate>().GetByIdAsync(templateId);
                if (template != null)
                {
                    await _unitOfWork.Repository<SubscriptionPlanTemplate>().AddAsync(new SubscriptionPlanTemplate
                    {
                        SubscriptionPlanId = plan.Id,
                        CardTemplateId = templateId
                    });
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        var dto = _mapper.Map<SubscriptionPlanAdminDto>(plan);
        return ServiceResult<SubscriptionPlanAdminDto>.Success(dto, _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<SubscriptionPlanAdminDto>> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanRequest request)
    {
        var plan = await _unitOfWork.Repository<SubscriptionPlan>().GetByIdAsync(planId);
        if (plan == null)
            return ServiceResult<SubscriptionPlanAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        if (!string.IsNullOrWhiteSpace(request.NameAr))
        {
            var trimmedAr = request.NameAr.Trim();
            var nameArExists = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AnyAsync(p => p.NameAr.Trim() == trimmedAr && p.Id != planId);
            if (nameArExists)
                return ServiceResult<SubscriptionPlanAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        if (!string.IsNullOrWhiteSpace(request.NameEn))
        {
            var trimmedEn = request.NameEn.Trim();
            var nameEnExists = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AnyAsync(p => p.NameEn.Trim() == trimmedEn && p.Id != planId);
            if (nameEnExists)
                return ServiceResult<SubscriptionPlanAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

        _mapper.Map(request, plan);
        _unitOfWork.Repository<SubscriptionPlan>().Update(plan);

        if (request.TemplateIds != null)
        {
            var existingAssignments = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
                .GetQueryable()
                .Where(pt => pt.SubscriptionPlanId == planId)
                .ToListAsync();

            foreach (var existing in existingAssignments)
            {
                _unitOfWork.Repository<SubscriptionPlanTemplate>().Remove(existing);
            }

            foreach (var templateId in request.TemplateIds)
            {
                await _unitOfWork.Repository<SubscriptionPlanTemplate>().AddAsync(new SubscriptionPlanTemplate
                {
                    SubscriptionPlanId = planId,
                    CardTemplateId = templateId
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<SubscriptionPlanAdminDto>(plan);
        return ServiceResult<SubscriptionPlanAdminDto>.Success(dto, _messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult> DeletePlanAsync(Guid planId)
    {
        var plan = await _unitOfWork.Repository<SubscriptionPlan>().GetByIdAsync(planId);
        if (plan == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        var hasSubscriptions = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable().AnyAsync(s => s.SubscriptionPlanId == planId && s.IsActive);

        if (hasSubscriptions)
            return ServiceResult.Fail(_messageService.Get("PlanHasActiveSubscriptions"), 409);

        _unitOfWork.Repository<SubscriptionPlan>().Remove(plan);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordDeleted"));
    }

    public async Task<ServiceResult<IReadOnlyList<CardTemplateSummaryDto>>> GetPlanTemplatesAsync(Guid planId)
    {
        var templates = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
            .GetQueryable()
            .AsNoTracking()
            .Include(pt => pt.CardTemplate)
            .Where(pt => pt.SubscriptionPlanId == planId)
            .Select(pt => pt.CardTemplate)
            .ToListAsync();

        var dtos = _mapper.Map<IReadOnlyList<CardTemplateSummaryDto>>(templates);
        return ServiceResult<IReadOnlyList<CardTemplateSummaryDto>>.Success(dtos);
    }

    public async Task<ServiceResult> AssignTemplateAsync(Guid planId, Guid templateId)
    {
        var plan = await _unitOfWork.Repository<SubscriptionPlan>().GetByIdAsync(planId);
        if (plan == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        var template = await _unitOfWork.Repository<CardTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        var exists = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
            .GetQueryable()
            .AsNoTracking()
            .AnyAsync(pt => pt.SubscriptionPlanId == planId && pt.CardTemplateId == templateId);

        if (exists)
            return ServiceResult.Fail(_messageService.Get("TemplateAlreadyAssigned"), 409);

        var assignment = new SubscriptionPlanTemplate
        {
            SubscriptionPlanId = planId,
            CardTemplateId = templateId
        };

        await _unitOfWork.Repository<SubscriptionPlanTemplate>().AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult> UnassignTemplateAsync(Guid planId, Guid templateId)
    {
        var assignment = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
            .GetQueryable()
            .FirstOrDefaultAsync(pt => pt.SubscriptionPlanId == planId && pt.CardTemplateId == templateId);

        if (assignment == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        _unitOfWork.Repository<SubscriptionPlanTemplate>().Remove(assignment);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("TemplateUnassigned"));
    }

    // Helpers
    private static bool IsValidStatusTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next) return true;
        return current switch
        {
            OrderStatus.PendingReview     => next is OrderStatus.UnderReview or OrderStatus.InPrinting or OrderStatus.ReadyForDelivery or OrderStatus.Approved or OrderStatus.Rejected or OrderStatus.Cancelled,
            OrderStatus.UnderReview       => next is OrderStatus.InPrinting or OrderStatus.Approved or OrderStatus.ReadyForDelivery or OrderStatus.Cancelled,
            OrderStatus.InPrinting        => next is OrderStatus.Encoding or OrderStatus.ReadyForDelivery or OrderStatus.Cancelled,
            OrderStatus.Encoding          => next is OrderStatus.ReadyForDelivery or OrderStatus.Cancelled,
            OrderStatus.ReadyForDelivery  => next is OrderStatus.Delivered or OrderStatus.Cancelled,
            OrderStatus.Delivered         => false,
            OrderStatus.Rejected          => false,
            OrderStatus.Cancelled         => false,
            _                             => false,
        };
    }

    private static string GenerateOtp() => Random.Shared.Next(100000, 999999).ToString();

    private void EnqueueOtpNotifications(User recipient, string otp, string cardName, bool isResend)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (string.IsNullOrWhiteSpace(culture)) culture = "ar";

        var templateKey = isResend ? "WhatsAppNewOtp" : "WhatsAppOrderReady";

        if (!string.IsNullOrWhiteSpace(recipient.Email))
            _backgroundJobClient.Enqueue<IEmailService>(x =>
                x.SendOrderReadyOtpEmailAsync(recipient.Email, otp, cardName, culture));

        var whatsAppNumber = recipient.UserProfile?.WhatsApp;
        if (!string.IsNullOrWhiteSpace(whatsAppNumber))
            _backgroundJobClient.Enqueue<IWhatsAppService>(x =>
                x.SendWhatsAppMessageAsync(whatsAppNumber, _messageService.Get(templateKey, otp)));
    }
}
