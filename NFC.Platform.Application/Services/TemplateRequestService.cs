namespace NFC.Platform.Application.Services;

    public class TemplateRequestService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant) : ITemplateRequestService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public async Task<ServiceResult<TemplateRequestDto>> CreateRequestAsync(Guid userId, CreateTemplateRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
            {
                var msg = _messageService.Get("Unauthorized");
                return ServiceResult<TemplateRequestDto>.Unauthorized(string.IsNullOrWhiteSpace(msg) ? "User is not authenticated." : msg);
            }

            // 1. Check custom design request limit
            var activeSub = await SubscriptionHelper.GetActiveSubWithPlanAsync(_unitOfWork, tenantId.Value);

            if (activeSub == null)
                return ServiceResult<TemplateRequestDto>.Fail(_messageService.Get("SubscriptionExpiredOrMissing"), 400);

            var limit = activeSub.SubscriptionPlan.MaxCustomDesignRequests;
            if (limit != SubscriptionConstants.UnlimitedQuota && activeSub.CustomDesignRequestsUsed >= limit)
                return ServiceResult<TemplateRequestDto>.Fail(_messageService.Get("CustomDesignRequestLimitReached"), 400);

            // 2. Create the request
            var templateRequest = _mapper.Map<TemplateRequest>(request);
            templateRequest.RequestedByUserId = userId;

            await _unitOfWork.Repository<TemplateRequest>().AddAsync(templateRequest);

            // 3. Increment counter
            activeSub.CustomDesignRequestsUsed++;

            await _unitOfWork.SaveChangesAsync();

            // Fetch with User details to return username
            var createdRequest = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == templateRequest.Id);

            var dto = _mapper.Map<TemplateRequestDto>(createdRequest);
            return ServiceResult<TemplateRequestDto>.Success(dto, _messageService.Get("RecordCreated"));
        }

        public async Task<ServiceResult<TemplateRequestDto>> UpdateRequestAsync(Guid id, Guid userId, UpdateTemplateRequest request)
        {
            var templateRequest = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (templateRequest == null)
            {
                return ServiceResult<TemplateRequestDto>.NotFound(_messageService.Get("RecordNotFound"));
            }

            if (templateRequest.Status != TemplateRequestStatus.Pending)
            {
                return ServiceResult<TemplateRequestDto>.Fail(_messageService.Get("TemplateRequestCannotBeUpdated"), 400);
            }

            _mapper.Map(request, templateRequest);

            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<TemplateRequestDto>(templateRequest);
            return ServiceResult<TemplateRequestDto>.Success(dto, _messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult<bool>> CancelRequestAsync(Guid id)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
            {
                var msg = _messageService.Get("Unauthorized");
                return ServiceResult<bool>.Unauthorized(string.IsNullOrWhiteSpace(msg) ? "User is not authenticated." : msg);
            }

            var templateRequest = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (templateRequest == null)
            {
                return ServiceResult<bool>.NotFound(_messageService.Get("RecordNotFound"));
            }

            if (templateRequest.Status != TemplateRequestStatus.Pending)
            {
                return ServiceResult<bool>.Fail(_messageService.Get("TemplateRequestCannotBeCancelled"), 400);
            }

            templateRequest.Status = TemplateRequestStatus.Cancelled;

            // Refund quota
            var activeSub = await SubscriptionHelper.GetActiveSubWithPlanAsync(_unitOfWork, tenantId.Value);
            if (activeSub != null && activeSub.CustomDesignRequestsUsed > 0)
            {
                activeSub.CustomDesignRequestsUsed--;
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<bool>.Success(true, _messageService.Get("TemplateRequestCancelled"));
        }


        public async Task<ServiceResult<IReadOnlyList<TemplateRequestDto>>> GetTenantRequestsAsync()
        {
            var requests = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Include(r => r.Tenant)
                .Include(r => r.RequestedByUser)
                    .ThenInclude(u => u.UserProfile)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<IReadOnlyList<TemplateRequestDto>>(requests);
            return ServiceResult<IReadOnlyList<TemplateRequestDto>>.Success(dtos);
        }


        public async Task<ServiceResult<TemplateRequestDto>> GetRequestByIdAsync(Guid id)
        {
            var request = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Include(r => r.Tenant)
                .Include(r => r.RequestedByUser)
                    .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return ServiceResult<TemplateRequestDto>.NotFound(_messageService.Get("RecordNotFound"));
            }

            var dto = _mapper.Map<TemplateRequestDto>(request);
            return ServiceResult<TemplateRequestDto>.Success(dto);
        }
    }

