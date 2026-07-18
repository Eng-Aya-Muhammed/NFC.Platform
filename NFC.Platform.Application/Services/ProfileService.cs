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
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound") ?? "User not found.");

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
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound") ?? "User not found.");

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = userId, TenantId = user.TenantId };
                await _unitOfWork.Repository<UserProfile>().AddAsync(user.UserProfile);
                await _unitOfWork.SaveChangesAsync();
            }

            _mapper.Map(request, user.UserProfile);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user), _messageService.Get("RecordUpdated") ?? "Profile updated successfully.");
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
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound") ?? "User not found.");

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = userId, TenantId = user.TenantId };
                await _unitOfWork.Repository<UserProfile>().AddAsync(user.UserProfile);
                await _unitOfWork.SaveChangesAsync();
            }

            user.UserProfile.UpdateCustomLinks(request.Links);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user), _messageService.Get("RecordUpdated") ?? "Links synchronized successfully.");
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> UpdateProfileTemplateAsync(Guid userId, UpdateUserProfileTemplateRequest request)
        {
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p!.CustomLinks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound") ?? "User not found.");

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = userId, TenantId = user.TenantId };
                await _unitOfWork.Repository<UserProfile>().AddAsync(user.UserProfile);
                await _unitOfWork.SaveChangesAsync();
            }

            // Apply only the fields that were explicitly provided
            if (request.ProfileTemplateId.HasValue) user.UserProfile.ProfileTemplateId = request.ProfileTemplateId;

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user), _messageService.Get("RecordUpdated") ?? "Template updated successfully.");
        }
    }
