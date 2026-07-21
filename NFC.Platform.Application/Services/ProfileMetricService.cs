using System.Text.Json;

namespace NFC.Platform.Application.Services;

public class ProfileMetricService(IUnitOfWork unitOfWork, IMessageService messageService, IMapper mapper) : IProfileMetricService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<ServiceResult<EmployeeDetailsDto>> ResolvePublicProfileAsync(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("ProfileNotFound"));

        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .Include(p => p.CustomLinks)
            .Include(p => p.Employee)
                .ThenInclude(e => e!.Company)
                    .ThenInclude(co => co!.ProfileTemplate)
            .Include(p => p.ProfileTemplate)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Subdomain == subdomain && !p.IsDeleted);

        if (profile == null)
            return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("ProfileNotFound"));

        var dto = _mapper.Map<EmployeeDetailsDto>(profile);
        dto.ProfileId = profile.Id;

        // Resolve logo URL for company employees
        string? logoUrl = null;
        if (profile.Employee?.Company != null)
        {
            var completedRequest = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Where(r => r.TenantId == profile.TenantId
                         && r.Status == TemplateRequestStatus.Completed
                         && r.RequestType == TemplateRequestType.ProfileTemplate)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            logoUrl = completedRequest?.LogoUrl;
        }

        ApplyBranding(dto, profile, logoUrl);

        return ServiceResult<EmployeeDetailsDto>.Success(dto);
    }

    public async Task<ServiceResult> RecordMetricAsync(Guid profileId, RecordMetricRequest request)
    {
        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetByIdAsync(profileId);

        if (profile == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound") ?? "Profile not found.");

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
