using System;
using System.Collections.Generic;
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

namespace NFC.Platform.Application.Services
{
    public class CardTemplateService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService) : ICardTemplateService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        public async Task<ServiceResult<IReadOnlyList<CardTemplateDto>>> GetActiveTemplatesAsync()
        {
            var templates = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var dtos = _mapper.Map<IReadOnlyList<CardTemplateDto>>(templates);
            return ServiceResult<IReadOnlyList<CardTemplateDto>>.Success(dtos);
        }

        public async Task<ServiceResult> SelectTemplateAsync(Guid userId, Guid templateId)
        {
            var template = await _unitOfWork.Repository<CardTemplate>().GetByIdAsync(templateId);
            if (template == null)
            {
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound") ?? "Template not found.");
            }

            if (!template.IsActive)
            {
                var msg = _messageService.Get("TemplateInactive");
                return ServiceResult.Fail(string.IsNullOrWhiteSpace(msg) ? "Template is inactive and cannot be selected." : msg, 400);
            }

            var profileRepo = _unitOfWork.Repository<UserProfile>();
            var profiles = await profileRepo.FindAsync(p => p.UserId == userId);
            var profile = profiles.Count > 0 ? profiles[0] : null;

            if (profile == null)
            {
                var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult.NotFound("User not found.");
                }

                profile = new UserProfile
                {
                    UserId = userId,
                    TenantId = user.TenantId,
                    FullName = user.Username,
                    CardTemplateId = templateId
                };

                await profileRepo.AddAsync(profile);
            }
            else
            {
                profile.CardTemplateId = templateId;
                profileRepo.Update(profile);
            }

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success(_messageService.Get("RecordUpdated") ?? "Template selected successfully.");
        }
    }
}
