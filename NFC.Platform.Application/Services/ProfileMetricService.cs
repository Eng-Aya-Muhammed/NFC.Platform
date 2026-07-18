using System.Text.Json;

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
                    .ThenInclude(e => e!.Company)
                        .ThenInclude(co => co!.ProfileTemplate)
            .Include(c => c.UserProfile)
                .ThenInclude(p => p!.ProfileTemplate)
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

            // Resolve Logo Url asynchronously if it's a company employee
            string? logoUrl = null;
            if (card.UserProfile.Employee?.Company != null)
            {
                var tenantId = card.UserProfile.TenantId;
                var completedRequest = await _unitOfWork.Repository<TemplateRequest>()
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(r => r.TenantId == tenantId && r.Status == TemplateRequestStatus.Completed)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                logoUrl = completedRequest?.LogoUrl;
            }

            // Resolve branding — priority: Company > Individual > Default
            ApplyBranding(dto, card.UserProfile, logoUrl);

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

    /// <summary>
    /// Resolves branding for the public profile with priority:
    ///   1. Company (if employee) — uses Resolved Company template
    ///   2. Individual — uses UserProfile.ProfileTemplate
    ///   3. Default fallback — neutral colors, no logo, "classic" layout
    /// </summary>
    private static void ApplyBranding(EmployeeDetailsDto dto, UserProfile profile, string? logoUrl)
    {
        CardTemplate? resolvedTemplate = null;

        var company = profile.Employee?.Company;
        if (company != null)
        {
            // Employee profile — use company branding template
            resolvedTemplate = company.ProfileTemplate;
        }
        else if (profile.ProfileTemplate != null)
        {
            // Individual profile — use own template
            resolvedTemplate = profile.ProfileTemplate;
        }

        // Extract layout from the resolved template's StyleConfigJson
        string? layout = null;
        string? styleConfigJson = null;

        if (resolvedTemplate != null && !string.IsNullOrWhiteSpace(resolvedTemplate.StyleConfigJson))
        {
            styleConfigJson = resolvedTemplate.StyleConfigJson;
            try
            {
                using var doc = JsonDocument.Parse(resolvedTemplate.StyleConfigJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("layout", out var lp)) layout = lp.GetString();
            }
            catch (JsonException) { /* malformed JSON — fall back to defaults */ }
        }

        // Final fallback defaults
        dto.LogoUrl = logoUrl;
        dto.Layout = layout ?? "classic";
        dto.StyleConfigJson = styleConfigJson;
    }
}
