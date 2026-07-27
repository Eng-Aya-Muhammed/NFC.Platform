namespace NFC.Platform.Application.Services;

public class ProfileMetricService(IUnitOfWork unitOfWork, IMessageService messageService, IMapper mapper) : IProfileMetricService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<ServiceResult<EmployeeDetailsDto>> ResolvePublicProfileAsync(Guid profileId)
    {
        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .Include(p => p.CustomLinks)
            .Include(p => p.Employee)
                .ThenInclude(e => e!.Company)
                    .ThenInclude(co => co!.ProfileTemplate)
            .Include(p => p.ProfileTemplate)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == profileId && !p.IsDeleted);

        if (profile == null)
            return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("ProfileNotFound"));

        var dto = _mapper.Map<EmployeeDetailsDto>(profile);
        dto.ProfileId = profile.Id;

        ApplyBranding(dto, profile);

        return ServiceResult<EmployeeDetailsDto>.Success(dto);
    }

    public async Task<ServiceResult> RecordMetricAsync(Guid profileId, RecordMetricRequest request)
    {
        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetByIdAsync(profileId);

        if (profile == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        var metric = _mapper.Map<ProfileMetric>(request);
        metric.UserProfileId = profileId;
        metric.TenantId = profile.TenantId;

        await _unitOfWork.Repository<ProfileMetric>().AddAsync(metric);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    /// <summary>
    /// Resolves branding for the public profile with priority:
    ///   1. Company (if employee) — uses Resolved Company template
    ///   2. Individual — uses UserProfile.ProfileTemplate
    ///   3. Default fallback — neutral colors, no logo, "classic" layout
    /// </summary>
    private static void ApplyBranding(EmployeeDetailsDto dto, UserProfile profile)
    {
        var company = profile.Employee?.Company;

        dto.CompanyName = company?.Name ?? profile.CompanyName;
        dto.LogoUrl = company?.LogoUrl ?? profile.ProfilePictureUrl;
        dto.ProfilePictureUrl = profile.ProfilePictureUrl;
        dto.Address = !string.IsNullOrWhiteSpace(profile.Address) ? profile.Address : (company?.Address ?? string.Empty);

        CardTemplate? resolvedTemplate = company?.ProfileTemplate ?? profile.ProfileTemplate;

        if (resolvedTemplate != null)
        {
            dto.Layout = resolvedTemplate.PhotoUrl;
            dto.StyleConfigJson = resolvedTemplate.FileUrl;
        }
    }
}

