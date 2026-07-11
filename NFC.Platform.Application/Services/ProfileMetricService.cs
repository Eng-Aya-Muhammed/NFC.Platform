using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Services;

public class ProfileMetricService : IProfileMetricService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly IMapper _mapper;

    public ProfileMetricService(IUnitOfWork unitOfWork, IMessageService messageService, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<ServiceResult<EmployeeDetailsDto>> ResolvePublicProfileAsync(string activationCode)
    {
        if (string.IsNullOrWhiteSpace(activationCode))
            return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("CardNotFound") ?? "Card not found.");

        // Fetch the card and its active linked user profile
        var card = await _unitOfWork.Repository<Card>()
            .GetQueryable()
            .Include(c => c.UserProfile)
                .ThenInclude(p => p!.CustomLinks)
            .Include(c => c.UserProfile)
                .ThenInclude(p => p!.Employee)
            .Include(c => c.UserProfile)
                .ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(c => c.ActivationCode == activationCode && c.IsActive);

        if (card == null || card.UserProfile == null)
            return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("CardNotFound") ?? "Card not found.");

        var profile = card.UserProfile;
        var dto = _mapper.Map<EmployeeDetailsDto>(profile);

        return ServiceResult<EmployeeDetailsDto>.Success(dto);
    }

    public async Task<ServiceResult> RecordMetricAsync(Guid profileId, RecordMetricRequest request, string? ipAddress, string? userAgent)
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
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ProfileLinkId = request.ProfileLinkId
        };

        await _unitOfWork.Repository<ProfileMetric>().AddAsync(metric);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }
}
