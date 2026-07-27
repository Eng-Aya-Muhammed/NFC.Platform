using NFC.Platform.Application.DTOs.Subscription;

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

        return ServiceResult<IReadOnlyList<SubscriptionPlanDto>>.Success(dtos);
    }

    public async Task<ServiceResult<UserSubscriptionDto>> GetCurrentSubscriptionAsync()
    {
        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return ServiceResult<UserSubscriptionDto>.Unauthorized(_messageService.Get("Unauthorized"));

        var activeSub = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .AsNoTracking()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (activeSub == null)
            return ServiceResult<UserSubscriptionDto>.NotFound(_messageService.Get("SubscriptionExpiredOrMissing"));

        var dto = _mapper.Map<UserSubscriptionDto>(activeSub);

        return ServiceResult<UserSubscriptionDto>.Success(dto);
    }

    public async Task<ServiceResult<IReadOnlyList<UserSubscriptionDto>>> GetSubscriptionHistoryAsync()
    {
        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return ServiceResult<IReadOnlyList<UserSubscriptionDto>>.Unauthorized(_messageService.Get("Unauthorized"));

        var history = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .AsNoTracking()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.TenantId == tenantId.Value)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var dtos = _mapper.Map<IReadOnlyList<UserSubscriptionDto>>(history);

        return ServiceResult<IReadOnlyList<UserSubscriptionDto>>.Success(dtos);
    }

    public async Task<ServiceResult<UserSubscriptionDto>> SubscribeAsync(SubscribeRequest request)
    {
        var tenantId = _currentTenant.TenantId;
        var userId = _currentTenant.UserId;

        if (!tenantId.HasValue || !userId.HasValue)
            return ServiceResult<UserSubscriptionDto>.Unauthorized(_messageService.Get("Unauthorized"));

        var plan = await _unitOfWork.Repository<SubscriptionPlan>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId);

        if (plan == null)
            return ServiceResult<UserSubscriptionDto>.NotFound(_messageService.Get("RecordNotFound"));

        // Check if there is an active subscription
        var activeSub = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow);

        if (activeSub != null)
        {
            return ServiceResult<UserSubscriptionDto>.Fail(_messageService.Get("HasActiveSubscription"), 400);
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

        return ServiceResult<UserSubscriptionDto>.Success(dto, _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<UserSubscriptionDto>> RenewSubscriptionAsync(RenewSubscriptionRequest request)
    {
        var tenantId = _currentTenant.TenantId;
        var userId = _currentTenant.UserId;

        if (!tenantId.HasValue || !userId.HasValue)
            return ServiceResult<UserSubscriptionDto>.Unauthorized(_messageService.Get("Unauthorized"));

        var plan = await _unitOfWork.Repository<SubscriptionPlan>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId);

        if (plan == null)
            return ServiceResult<UserSubscriptionDto>.NotFound(_messageService.Get("RecordNotFound"));

        // Find current active subscription
        var activeSub = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow);

        if (activeSub == null)
        {
            return ServiceResult<UserSubscriptionDto>.Fail(_messageService.Get("NoActiveSubscriptionToRenew"), 400);
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

        return ServiceResult<UserSubscriptionDto>.Success(dto, _messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<UserSubscriptionDto>> AdminExtendSubscriptionAsync(Guid tenantId, ExtendSubscriptionRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.ExtensionDays < 1 || request.ExtensionDays > 3650)
            return ServiceResult<UserSubscriptionDto>.Fail(_messageService.Get("InvalidExtensionDays"), 400);

        var tenantRepo = _unitOfWork.Repository<Tenant>();
        var tenant = await tenantRepo.GetQueryable().AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);
        if (tenant == null)
            return ServiceResult<UserSubscriptionDto>.NotFound(_messageService.Get("RecordNotFound"));

        // Target the single tenant subscription directly, bypassing tenant context query filters for Admin operations
        var sub = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted);

        if (sub == null)
            return ServiceResult<UserSubscriptionDto>.Fail(_messageService.Get("NoSubscriptionFoundToExtend"), 400);

        var isExpired = sub.EndDate <= DateTime.UtcNow;

        if (isExpired)
        {
            sub.StartDate = DateTime.UtcNow;
            sub.EndDate = DateTime.UtcNow.AddDays(request.ExtensionDays);
        }
        else
        {
            // Active subscription: Keep original StartDate, extend EndDate from current EndDate
            sub.EndDate = sub.EndDate.AddDays(request.ExtensionDays);
        }

        sub.IsActive = true;
        // Usage quotas (TemplateChangesUsed, CustomDesignRequestsUsed) are preserved intact

        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<UserSubscriptionDto>(sub);
        return ServiceResult<UserSubscriptionDto>.Success(dto, _messageService.Get("SubscriptionExtendedSuccessfully"));
    }
}
