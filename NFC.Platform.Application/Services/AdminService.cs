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

        public async Task<ServiceResult<PagedResult<AdminOrderSummaryDto>>> GetOrdersPagedAsync(PaginationRequest request, OrderStatus? statusFilter, Guid? companyId = null)
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

            var pagedResult = await query.ToPagedResultAsync(request, o => _mapper.Map<AdminOrderSummaryDto>(o));
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
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return ServiceResult<AdminOrderDetailDto>.NotFound(_messageService.Get("RecordNotFound"));

            return ServiceResult<AdminOrderDetailDto>.Success(_mapper.Map<AdminOrderDetailDto>(order));
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

            // Validate forward-only transition
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

            // ── OTP notification when order is ready for delivery ──────────────────────
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

            // Check if OTP has expired
            if (order.DeliveryOtpExpiresAt.HasValue && order.DeliveryOtpExpiresAt.Value < DateTime.UtcNow)
                return ServiceResult.Fail(_messageService.Get("OtpExpired"), 422);

            if (order.DeliveryOtp != otp)
                return ServiceResult.Fail(_messageService.Get("InvalidOtp"), 422);

            // OTP is correct — mark as delivered and clear tracking state
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

            // Enforce 60-second cooldown rate limit
            if (order.DeliveryOtpLastSentAt.HasValue &&
                (DateTime.UtcNow - order.DeliveryOtpLastSentAt.Value).TotalSeconds < 60)
            {
                return ServiceResult.Fail(_messageService.Get("OtpCooldownActive"), 422);
            }

            // Enforce maximum 5 resend attempts per order
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

        private void EnqueueOtpNotifications(User recipient, string otp, string cardName, bool isResend)
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (string.IsNullOrWhiteSpace(culture)) culture = "ar";

            // Email job via Hangfire
            if (!string.IsNullOrWhiteSpace(recipient.Email))
            {
                _backgroundJobClient.Enqueue<IEmailService>(x =>
                    x.SendOrderReadyOtpEmailAsync(recipient.Email, otp, cardName, culture));
            }

            // WhatsApp job via Hangfire
            var whatsAppNumber = recipient.UserProfile?.WhatsApp;
            if (!string.IsNullOrWhiteSpace(whatsAppNumber))
            {
                var messageKey = isResend ? "WhatsAppNewOtp" : "WhatsAppOrderReady";
                var waMessage = _messageService.Get(messageKey, otp);
                _backgroundJobClient.Enqueue<IWhatsAppService>(x =>
                    x.SendWhatsAppMessageAsync(whatsAppNumber, waMessage));
            }
        }

        private static string GenerateOtp()
            => Random.Shared.Next(100000, 999999).ToString();

        private bool IsValidStatusTransition(OrderStatus current, OrderStatus next)
        {
            // Rejected is allowed from PendingReview or UnderReview only
            if (next == OrderStatus.Rejected)
                return current == OrderStatus.PendingReview || current == OrderStatus.UnderReview;

            // All other transitions must be strictly forward
            return (int)next > (int)current;
        }


        public async Task<ServiceResult<PagedResult<TemplateRequestDto>>> GetTemplateRequestsPagedAsync(PaginationRequest request, TemplateRequestStatus? status = null)
        {
            var query = _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Include(tr => tr.RequestedByUser)
                .OrderByDescending(tr => tr.CreatedAt)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(tr => tr.Status == status.Value);
            }

            var pagedResult = await query.ToPagedResultAsync(request, tr => _mapper.Map<TemplateRequestDto>(tr));
            return ServiceResult<PagedResult<TemplateRequestDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult> ResolveTemplateRequestAsync(Guid id, ResolveTemplateRequestDto dto)
        {
            var requestRepo = _unitOfWork.Repository<TemplateRequest>();
            var templateRequest = await requestRepo.GetQueryable()
                .Include(r => r.RequestedByUser)
                .Include(r => r.Tenant)
                    .ThenInclude(t => t.Company)
                        .ThenInclude(c => c!.AdminUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (templateRequest == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                templateRequest.Status = dto.Status;
                if (!string.IsNullOrWhiteSpace(dto.Notes))
                {
                    templateRequest.Notes = string.IsNullOrWhiteSpace(templateRequest.Notes)
                        ? dto.Notes
                        : $"{templateRequest.Notes}\nAdmin Notes: {dto.Notes}";
                }

                if (dto.Status == TemplateRequestStatus.Completed)
                {
                    var customTemplate = new CardTemplate
                    {
                        TenantId = templateRequest.TenantId,
                        Name = templateRequest.TemplateName,
                        Category = _messageService.Get("CustomCategory"),
                        ThumbnailUrl = templateRequest.ReferenceImageUrl ?? templateRequest.LogoUrl ?? "",
                        StyleConfigJson = dto.StyleConfigJson ?? "{}",
                        IsActive = true,
                        DisplayOrder = 1
                    };

                    await _unitOfWork.Repository<CardTemplate>().AddAsync(customTemplate);

                    // Fill in the produced template reference
                    templateRequest.ProducedTemplateId = customTemplate.Id;

                    // Auto-apply the new template + branding to the requesting tenant
                    // Try Company first (company tenant), then fall back to UserProfile (individual tenant)
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

                // Enqueue email notification to requesting user/admin when request is completed
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



        public async Task<ServiceResult<CardTemplateDto>> CreateTemplateAsync(CreateCardTemplateDto dto)
        {
            var template = _mapper.Map<CardTemplate>(dto);
            template.TenantId = null;

            await _unitOfWork.Repository<CardTemplate>().AddAsync(template);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<CardTemplateDto>(template);
            return ServiceResult<CardTemplateDto>.Success(resultDto);
        }

        public async Task<ServiceResult<CardTemplateDto>> UpdateTemplateAsync(Guid id, UpdateCardTemplateDto dto)
        {
            var templateRepo = _unitOfWork.Repository<CardTemplate>();
            var template = await templateRepo.GetByIdAsync(id);

            if (template == null)
                return ServiceResult<CardTemplateDto>.NotFound(_messageService.Get("RecordNotFound"));

            _mapper.Map(dto, template);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<CardTemplateDto>(template);
            return ServiceResult<CardTemplateDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteTemplateAsync(Guid id)
        {
            var templateRepo = _unitOfWork.Repository<CardTemplate>();
            var template = await templateRepo.GetByIdAsync(id);

            if (template == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            template.IsActive = !template.IsActive;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult<PagedResult<TenantSummaryDto>>> GetTenantsPagedAsync(PaginationRequest request)
        {
            var query = _unitOfWork.Repository<Tenant>()
                .GetQueryable()
                .AsNoTracking()
                .Include(t => t.Company)
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            var pagedTenants = await query.ToPagedResultAsync(request, t => t);

            var tenantSummaryDtos = new List<TenantSummaryDto>();
            foreach (var tenant in pagedTenants.Items)
            {
                var dto = _mapper.Map<TenantSummaryDto>(tenant);
                dto.AccountType = tenant.Company != null ? "Company" : "Individual";

                var activeSub = await _unitOfWork.Repository<UserSubscription>()
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(us => us.SubscriptionPlan)
                    .Where(us => us.TenantId == tenant.Id && us.IsActive)
                    .OrderByDescending(us => us.EndDate)
                    .FirstOrDefaultAsync();

                if (activeSub != null)
                {
                    dto.ActivePlanName = activeSub.SubscriptionPlan?.Name;
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

        public async Task<ServiceResult> UpdateCardPricingAsync(UpdateCardPricingDto dto)
        {
            if (dto == null)
                return ServiceResult.Fail(_messageService.Get("InvalidRequest"), 400);

            var normalizedCurrency = dto.Currency?.Trim().ToUpper();
            if (string.IsNullOrEmpty(normalizedCurrency) || normalizedCurrency.Length != 3)
            {
                return ServiceResult.Fail(_messageService.Get("CurrencyMustBeThreeLetters"), 400);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var repo = _unitOfWork.Repository<CardPricing>();

                var activePricing = await repo.GetQueryable()
                    .FirstOrDefaultAsync(p => p.CardType == dto.CardType && p.IsActive);

                if (activePricing != null)
                {
                    if (activePricing.UnitPrice == dto.UnitPrice && activePricing.Currency == normalizedCurrency)
                    {
                        await _unitOfWork.CommitTransactionAsync();
                        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
                    }

                    activePricing.EffectiveTo = DateTime.UtcNow;
                    activePricing.IsActive = false;
                }

                var newPricing = new CardPricing
                {
                    CardType = dto.CardType,
                    UnitPrice = dto.UnitPrice,
                    Currency = normalizedCurrency,
                    EffectiveFrom = DateTime.UtcNow,
                    EffectiveTo = null,
                    IsActive = true
                };

                await repo.AddAsync(newPricing);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }

        // â”€â”€ Subdomain management (Super Admin) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public async Task<ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>> GetAllProfileSubdomainsAsync(
            PaginationRequest request, string? search)
        {
            var query = _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .AsNoTracking()
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.Company)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    (p.Subdomain != null && p.Subdomain.Contains(search)) ||
                    p.FullName.Contains(search));

            var pagedResult = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToPagedResultAsync(request, p => _mapper.Map<ProfileSubdomainSummaryDto>(p));

            return ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult> ReassignSubdomainAsync(Guid profileId, string newSubdomain)
        {
            var profile = await _unitOfWork.Repository<UserProfile>().GetByIdAsync(profileId);
            if (profile == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            var normalized = SubdomainHelper.Slugify(newSubdomain);

            var exists = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Subdomain == normalized && p.Id != profileId);

            if (exists)
                return ServiceResult.Fail(
                    _messageService.Get("SubdomainAlreadyTaken"), 409);

            profile.Subdomain = normalized;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }
    }

