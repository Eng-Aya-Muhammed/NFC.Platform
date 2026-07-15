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
        return card.Status switch
        {
            CardStatus.Active when card.UserProfile != null =>
                ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(card.UserProfile)),

            CardStatus.Active =>
                ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("ProfileNotFound")),

            CardStatus.Deactivated =>
                ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("CardDeactivated"), 410),

            _ => // UnassignedCode, Encoded, PendingGeneration
                ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("CardNotYetActivated"), 403)
        };
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
            ProfileLinkId = request.ProfileLinkId
        };

        await _unitOfWork.Repository<ProfileMetric>().AddAsync(metric);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }
}
