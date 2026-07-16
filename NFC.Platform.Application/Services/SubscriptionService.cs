namespace NFC.Platform.Application.Services;

    public class SubscriptionService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant) : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public async Task<ServiceResult<IReadOnlyList<SubscriptionPlanDto>>> GetPlansAsync()
        {
            var plans = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AsNoTracking()
                .OrderBy(p => p.DurationInDays)
                .ToListAsync();

            var dtos = _mapper.Map<IReadOnlyList<SubscriptionPlanDto>>(plans);

            foreach (var dto in dtos)
            {
                dto.Name = _messageService.Get(dto.Name);
                dto.Description = _messageService.Get(dto.Description);
            }

            return ServiceResult<IReadOnlyList<SubscriptionPlanDto>>.Success(dtos);
        }

        public async Task<ServiceResult<UserSubscriptionDto>> GetCurrentSubscriptionAsync()
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<UserSubscriptionDto>.Unauthorized(_messageService.Get("Unauthorized") ?? "User is not authenticated.");

            var activeSub = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            if (activeSub == null)
                return ServiceResult<UserSubscriptionDto>.NotFound(_messageService.Get("SubscriptionExpiredOrMissing") ?? "No active subscription found.");

            var dto = _mapper.Map<UserSubscriptionDto>(activeSub);
            dto.PlanName = _messageService.Get(dto.PlanName);

            return ServiceResult<UserSubscriptionDto>.Success(dto);
        }

        public async Task<ServiceResult<IReadOnlyList<UserSubscriptionDto>>> GetSubscriptionHistoryAsync()
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<IReadOnlyList<UserSubscriptionDto>>.Unauthorized(_messageService.Get("Unauthorized") ?? "User is not authenticated.");

            var history = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.TenantId == tenantId.Value)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<IReadOnlyList<UserSubscriptionDto>>(history);

            foreach (var dto in dtos)
            {
                dto.PlanName = _messageService.Get(dto.PlanName);
            }

            return ServiceResult<IReadOnlyList<UserSubscriptionDto>>.Success(dtos);
        }

        public async Task<ServiceResult<UserSubscriptionDto>> SubscribeAsync(SubscribeRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            var userId = _currentTenant.UserId;

            if (!tenantId.HasValue || !userId.HasValue)
                return ServiceResult<UserSubscriptionDto>.Unauthorized(_messageService.Get("Unauthorized") ?? "User is not authenticated.");

            var plan = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId);

            if (plan == null)
                return ServiceResult<UserSubscriptionDto>.NotFound(_messageService.Get("RecordNotFound") ?? "Plan not found.");

            // Check if there is an active subscription
            var activeSub = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow);

            if (activeSub != null)
            {
                return ServiceResult<UserSubscriptionDto>.Fail(_messageService.Get("HasActiveSubscription") ?? "You already have an active subscription. Please use the renew option instead.", 400);
            }

            var newSub = _mapper.Map<UserSubscription>(request);
            newSub.UserId = userId.Value;
            newSub.StartDate = DateTime.UtcNow;
            newSub.EndDate = DateTime.UtcNow.AddDays(plan.DurationInDays);
            newSub.IsActive = true;

            await _unitOfWork.Repository<UserSubscription>().AddAsync(newSub);
            await _unitOfWork.SaveChangesAsync();

            newSub.SubscriptionPlan = plan;

            var dto = _mapper.Map<UserSubscriptionDto>(newSub);
            dto.PlanName = _messageService.Get(dto.PlanName);

            return ServiceResult<UserSubscriptionDto>.Success(dto, _messageService.Get("RecordCreated") ?? "Subscribed successfully.");
        }

        public async Task<ServiceResult<UserSubscriptionDto>> RenewSubscriptionAsync(RenewSubscriptionRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            var userId = _currentTenant.UserId;

            if (!tenantId.HasValue || !userId.HasValue)
                return ServiceResult<UserSubscriptionDto>.Unauthorized(_messageService.Get("Unauthorized") ?? "User is not authenticated.");

            var plan = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId);

            if (plan == null)
                return ServiceResult<UserSubscriptionDto>.NotFound(_messageService.Get("RecordNotFound") ?? "Plan not found.");

            // Find current active subscription
            var activeSub = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow);

            if (activeSub == null)
            {
                return ServiceResult<UserSubscriptionDto>.Fail(_messageService.Get("NoActiveSubscriptionToRenew") ?? "No active subscription found to renew. Please subscribe first.", 400);
            }

            var newSub = _mapper.Map<UserSubscription>(request);
            newSub.UserId = userId.Value;
            newSub.StartDate = activeSub.EndDate;
            newSub.EndDate = activeSub.EndDate.AddDays(plan.DurationInDays);
            newSub.IsActive = true;

            await _unitOfWork.Repository<UserSubscription>().AddAsync(newSub);
            await _unitOfWork.SaveChangesAsync();

            // Load plan navigation properties for returned DTO
            newSub.SubscriptionPlan = plan;

            var dto = _mapper.Map<UserSubscriptionDto>(newSub);
            dto.PlanName = _messageService.Get(dto.PlanName);

            return ServiceResult<UserSubscriptionDto>.Success(dto, _messageService.Get("RecordUpdated") ?? "Subscription renewed successfully.");
        }
    }
