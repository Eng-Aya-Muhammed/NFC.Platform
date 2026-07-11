using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Services
{
    public class TemplateRequestService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant) : ITemplateRequestService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public async Task<ServiceResult<TemplateRequestDto>> CreateRequestAsync(Guid userId, CreateTemplateRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
            {
                var msg = _messageService.Get("Unauthorized");
                return ServiceResult<TemplateRequestDto>.Unauthorized(string.IsNullOrWhiteSpace(msg) ? "User is not authenticated." : msg);
            }

            var templateRequest = new TemplateRequest
            {
                TenantId = tenantId.Value,
                RequestedByUserId = userId,
                TemplateName = request.TemplateName,
                LogoUrl = request.LogoUrl,
                ReferenceImageUrl = request.ReferenceImageUrl,
                Notes = request.Notes,
                Status = TemplateRequestStatus.Pending
            };

            await _unitOfWork.Repository<TemplateRequest>().AddAsync(templateRequest);
            await _unitOfWork.SaveChangesAsync();

            // Fetch with User details to return username
            var createdRequest = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == templateRequest.Id);

            var dto = _mapper.Map<TemplateRequestDto>(createdRequest);
            return ServiceResult<TemplateRequestDto>.Success(dto, _messageService.Get("RecordCreated") ?? "Template request submitted successfully.");
        }

        public async Task<ServiceResult<IReadOnlyList<TemplateRequestDto>>> GetTenantRequestsAsync()
        {
            var requests = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .Include(r => r.RequestedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<IReadOnlyList<TemplateRequestDto>>(requests);
            return ServiceResult<IReadOnlyList<TemplateRequestDto>>.Success(dtos);
        }

        public async Task<ServiceResult<TemplateRequestDto>> UpdateRequestStatusAsync(Guid id, TemplateRequestStatus status)
        {
            var request = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return ServiceResult<TemplateRequestDto>.NotFound(_messageService.Get("RecordNotFound") ?? "Template request not found.");
            }

            request.Status = status;
            _unitOfWork.Repository<TemplateRequest>().Update(request);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<TemplateRequestDto>(request);
            return ServiceResult<TemplateRequestDto>.Success(dto, _messageService.Get("RecordUpdated") ?? "Request status updated successfully.");
        }
    }
}
