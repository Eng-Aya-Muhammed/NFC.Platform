namespace NFC.Platform.Application.Services;

    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly IQrCodeGenerator _qrCodeGenerator;
        private readonly IStorageService _storageService;

        public AdminService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            IQrCodeGenerator qrCodeGenerator,
            IStorageService storageService)
        {
            _unitOfWork       = unitOfWork       ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper           = mapper           ?? throw new ArgumentNullException(nameof(mapper));
            _messageService   = messageService   ?? throw new ArgumentNullException(nameof(messageService));
            _qrCodeGenerator  = qrCodeGenerator  ?? throw new ArgumentNullException(nameof(qrCodeGenerator));
            _storageService   = storageService   ?? throw new ArgumentNullException(nameof(storageService));
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
                return ServiceResult<AdminOrderDetailDto>.NotFound(_messageService.Get("RecordNotFound") ?? "Order not found.");

            return ServiceResult<AdminOrderDetailDto>.Success(_mapper.Map<AdminOrderDetailDto>(order));
        }

        public async Task<ServiceResult> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto)
        {
            var orderRepo = _unitOfWork.Repository<CardOrder>();
            var order = await orderRepo.GetQueryable()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound") ?? "Order not found.");

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

            var previousStatus = order.Status;
            order.Status = dto.Status;
            await _unitOfWork.SaveChangesAsync();

            // Step B: generate Card rows when moving into InPrinting
            if (dto.Status == OrderStatus.InPrinting && previousStatus != OrderStatus.InPrinting)
                await GenerateCardsForOrderAsync(order);

            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }

        private async Task GenerateCardsForOrderAsync(CardOrder order)
        {
            var cardRepo = _unitOfWork.Repository<Card>();
            var existingCardCodes = await cardRepo
                .GetQueryable()
                .AsNoTracking()
                .Where(c => c.CardOrderId == order.Id)
                .Select(c => c.UniqueCode)
                .ToListAsync();

            var existingSet = new HashSet<string>(existingCardCodes, StringComparer.OrdinalIgnoreCase);
            var newCards = new List<Card>();

            // ── Build card stubs ─────────────────────────────────────────────────────
            // Use OrderItems to create one Card per item (or fill up to Quantity if no items)
            if (order.Items.Count > 0)
            {
                foreach (var item in order.Items)
                {
                    var code = GenerateUniqueCode(existingSet);
                    var isCompanyCard = item.UserProfileId.HasValue;
                    newCards.Add(new Card
                    {
                        TenantId      = order.TenantId,
                        UniqueCode    = code,
                        ProfileUrl    = $"https://onpoint-teasting.com/c/{code}",
                        Status        = isCompanyCard ? CardStatus.Active : CardStatus.UnassignedCode,
                        ActivatedAt   = isCompanyCard ? DateTime.UtcNow : null,
                        CardOrderId   = order.Id,
                        UserProfileId = item.UserProfileId
                    });
                    existingSet.Add(code);
                }
            }
            else
            {
                for (var i = 0; i < order.Quantity; i++)
                {
                    var code = GenerateUniqueCode(existingSet);
                    newCards.Add(new Card
                    {
                        TenantId    = order.TenantId,
                        UniqueCode  = code,
                        ProfileUrl  = $"https://onpoint-teasting.com/c/{code}",
                        Status      = CardStatus.UnassignedCode,
                        CardOrderId = order.Id
                    });
                    existingSet.Add(code);
                }
            }

            // ── Generate QR PNGs (CPU-only, sync) then upload all in parallel ────────
            // Generating bytes is synchronous and fast (~1 ms/card).
            // Cloudinary uploads are I/O-bound — Task.WhenAll maximises throughput.
            var uploadTasks = newCards.Select(async card =>
            {
                var pngBytes = _qrCodeGenerator.GeneratePngBytes(card.ProfileUrl);
                var uploadResult = await _storageService.UploadBytesAsImageAsync(
                    pngBytes,
                    $"qr-{card.UniqueCode}.png",
                    $"qrcodes/{order.TenantId}");
                card.QrCodeUrl = uploadResult.SecureUrl;
            });

            await Task.WhenAll(uploadTasks);

            // ── Persist all cards in one SaveChanges call ────────────────────────────
            foreach (var card in newCards)
                await cardRepo.AddAsync(card);

            await _unitOfWork.SaveChangesAsync();
        }

        private string GenerateUniqueCode(HashSet<string> existingCodes)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            string code;
            do
            {
                code = new string(Enumerable.Range(0, 10).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
            }
            while (existingCodes.Contains(code));
            return code;
        }

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
            var templateRequest = await requestRepo.GetByIdAsync(id);

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
                        Category = _messageService.Get("CustomCategory") ?? "Custom",
                        ThumbnailUrl = templateRequest.ReferenceImageUrl ?? templateRequest.LogoUrl ?? "",
                        StyleConfigJson = dto.StyleConfigJson ?? "{}",
                        IsActive = true,
                        DisplayOrder = 1
                    };

                    await _unitOfWork.Repository<CardTemplate>().AddAsync(customTemplate);

                    // Fill in the produced template reference
                    templateRequest.ProducedTemplateId = customTemplate.Id;

                    // Cascade: advance the linked order from AwaitingDesign → PendingReview
                    if (templateRequest.LinkedOrderId.HasValue)
                    {
                        var linkedOrder = await _unitOfWork.Repository<CardOrder>()
                            .GetByIdAsync(templateRequest.LinkedOrderId.Value);

                        if (linkedOrder != null && linkedOrder.Status == OrderStatus.AwaitingDesign)
                        {
                            linkedOrder.Status = OrderStatus.PendingReview;
                        }
                    }

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
                return ServiceResult<CardTemplateDto>.NotFound(_messageService.Get("RecordNotFound") ?? "Template not found.");

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
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound") ?? "Template not found.");

            template.IsActive = !template.IsActive;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RecordUpdated") ?? "Template status updated successfully.");
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
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound") ?? "Tenant not found.");

            tenant.IsActive = dto.IsActive;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RecordUpdated") ?? "Tenant status updated successfully.");
        }

        public async Task<ServiceResult> UpdateCardPricingAsync(UpdateCardPricingDto dto)
        {
            if (dto == null)
                return ServiceResult.Fail(_messageService.Get("InvalidRequest") ?? "Invalid request payload.", 400);

            var normalizedCurrency = dto.Currency?.Trim().ToUpper();
            if (string.IsNullOrEmpty(normalizedCurrency) || normalizedCurrency.Length != 3)
            {
                return ServiceResult.Fail(_messageService.Get("CurrencyMustBeThreeLetters") ?? "Currency must be exactly 3 alphabetic characters.", 400);
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
                        return ServiceResult.Success(_messageService.Get("RecordUpdated") ?? "Pricing updated successfully.");
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

            return ServiceResult.Success(_messageService.Get("RecordUpdated") ?? "Pricing updated successfully.");
        }
    }
