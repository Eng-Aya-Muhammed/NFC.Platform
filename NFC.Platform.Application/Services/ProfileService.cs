namespace NFC.Platform.Application.Services;

    public class ProfileService : IProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public ProfileService(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> GetProfileAsync(Guid userId)
        {
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .AsNoTracking()
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p!.CustomLinks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> UpdateProfileAsync(Guid userId, UpdateMyProfileRequest request)
        {
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p!.CustomLinks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = userId, TenantId = user.TenantId };
                await _unitOfWork.Repository<UserProfile>().AddAsync(user.UserProfile);
                await _unitOfWork.SaveChangesAsync();
            }

            //  Subdomain update (slugify + global uniqueness check) 
            if (!string.IsNullOrWhiteSpace(request.Subdomain))
            {
                var normalized = SubdomainHelper.Slugify(request.Subdomain);

                if (!string.Equals(normalized, user.UserProfile.Subdomain, StringComparison.OrdinalIgnoreCase))
                {
                    var exists = await _unitOfWork.Repository<UserProfile>()
                        .GetQueryable()
                        .IgnoreQueryFilters()
                        .AnyAsync(p => p.Subdomain == normalized && p.Id != user.UserProfile.Id);

                    if (exists)
                        return ServiceResult<EmployeeDetailsDto>.Fail(
                            _messageService.Get("SubdomainAlreadyTaken"), 409);

                    user.UserProfile.Subdomain = normalized;
                }
            }

            _mapper.Map(request, user.UserProfile);

            if (request.Links?.Count > 0)
            {
                user.UserProfile.UpdateCustomLinks(request.Links);
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user), _messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> SynchronizeLinksAsync(Guid userId, SynchronizeLinksRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p!.CustomLinks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = userId, TenantId = user.TenantId };
                await _unitOfWork.Repository<UserProfile>().AddAsync(user.UserProfile);
                await _unitOfWork.SaveChangesAsync();
            }

            user.UserProfile.UpdateCustomLinks(request.Links);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user), _messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> UpdateProfileTemplateAsync(Guid userId, Guid? templateId)
        {
            if (!templateId.HasValue)
            {
                // Logic to remove the template and revert to default.
                // Assuming we just set the ProfileTemplateId to null.
                var userProfile = await _unitOfWork.Repository<UserProfile>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (userProfile != null)
                {
                    userProfile.ProfileTemplateId = null;
                    await _unitOfWork.SaveChangesAsync();
                }

                var u = await _unitOfWork.Repository<User>().GetQueryable()
                    .Include(x => x.UserProfile)
                        .ThenInclude(p => p!.CustomLinks)
                    .FirstOrDefaultAsync(x => x.Id == userId);
                    
                return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(u), _messageService.Get("RecordUpdated"));
            }

            // 1. Verify the requested template exists and is active
            var template = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive && !t.IsDeleted);

            if (template == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            // 2. Load user + profile
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p!.CustomLinks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = userId, TenantId = user.TenantId };
                await _unitOfWork.Repository<UserProfile>().AddAsync(user.UserProfile);
                await _unitOfWork.SaveChangesAsync();
            }

            // 3. Load active subscription (with plan templates for access check)
            var activeSub = await SubscriptionHelper.GetActiveSubWithTemplatesAsync(_unitOfWork, user.TenantId);

            if (activeSub == null)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("SubscriptionExpiredOrMissing"), 400);

            // 4. Check template is assigned to this plan
            var isTemplateAllowed = activeSub.SubscriptionPlan.PlanTemplates
                .Any(pt => pt.CardTemplateId == templateId);

            if (!isTemplateAllowed)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("TemplateNotAllowedInPlan"), 403);

            // 5. Check template-change limit (SubscriptionConstants.UnlimitedQuota = unlimited)
            var limit = activeSub.SubscriptionPlan.MaxTemplateChanges;
            if (limit != SubscriptionConstants.UnlimitedQuota && activeSub.TemplateChangesUsed >= limit)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("TemplateChangeLimitReached"), 400);

            // 6. Apply change + increment counter
            user.UserProfile.ProfileTemplateId = templateId;
            activeSub.TemplateChangesUsed++;

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user), _messageService.Get("RecordUpdated"));
        }
    }

