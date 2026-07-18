namespace NFC.Platform.Application.Services;

public class ProfileMetricService(IUnitOfWork unitOfWork, IMessageService messageService, IMapper mapper) : IProfileMetricService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<ServiceResult<EmployeeDetailsDto>> ResolvePublicProfileAsync(string activationCode)
    {
        if (string.IsNullOrWhiteSpace(activationCode))
            return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("CardNotFound"));

        var card = await _unitOfWork.Repository<Card>()
            .GetQueryable()
            .AsNoTracking()
            .Include(c => c.UserProfile)
                .ThenInclude(p => p!.CustomLinks)
            .Include(c => c.UserProfile)
                .ThenInclude(p => p!.Employee)
            .Include(c => c.UserProfile)
                .ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(c => c.UniqueCode == activationCode && !c.IsDeleted);

        if (card == null)
            return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("CardNotFound"));

        // Status-based resolution
        if (card.Status == CardStatus.Active)
        {
            if (card.UserProfile == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("ProfileNotFound"));

            var dto = _mapper.Map<EmployeeDetailsDto>(card.UserProfile);
            dto.CardId = card.Id;
            return ServiceResult<EmployeeDetailsDto>.Success(dto);
        }

        if (card.Status == CardStatus.Deactivated)
        {
            return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("CardDeactivated"), 410);
        }

        // UnassignedCode, Encoded, PendingGeneration
        return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("CardNotYetActivated"), 403);
    }

    public async Task<ServiceResult> RecordMetricAsync(Guid profileId, RecordMetricRequest request)
    {
        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetByIdAsync(profileId);

        if (profile == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound") ?? "Profile not found.");

        var metric = new ProfileMetric
        {
            UserProfileId = profileId,
            TenantId = profile.TenantId,
            InteractionType = request.InteractionType,
            ProfileLinkId = request.ProfileLinkId,
            CardId = request.CardId
        };

        await _unitOfWork.Repository<ProfileMetric>().AddAsync(metric);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }
}
