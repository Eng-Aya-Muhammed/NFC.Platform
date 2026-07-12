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
    }
}
