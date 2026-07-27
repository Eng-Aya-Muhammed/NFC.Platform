using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;


namespace NFC.Platform.Application.Services;

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
        var entities = await _unitOfWork.Repository<CardTemplate>()
            .GetQueryable()
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();

        var dtos = _mapper.Map<IReadOnlyList<CardTemplateDto>>(entities);
        return ServiceResult<IReadOnlyList<CardTemplateDto>>.Success(dtos);
    }

    public async Task<ServiceResult<PagedResult<CardTemplateAdminDto>>> GetAllAdminTemplatesAsync(PaginationRequest request)
    {
        var query = _unitOfWork.Repository<CardTemplate>()
            .GetQueryable()
            .AsNoTracking()
            .OrderBy(t => t.DisplayOrder);

        var pagedResult = await query.ToPagedResultAsync(request, t => _mapper.Map<CardTemplateAdminDto>(t));
        return ServiceResult<PagedResult<CardTemplateAdminDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<CardTemplateAdminDto>> GetByIdAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<CardTemplate>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<CardTemplateAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<CardTemplateAdminDto>(entity);
        return ServiceResult<CardTemplateAdminDto>.Success(dto);
    }

    public async Task<ServiceResult<CardTemplateAdminDto>> CreateAsync(CreateCardTemplateRequest request)
    {
        var category = await _unitOfWork.Repository<TemplateCategory>().GetByIdAsync(request.CategoryId);
        if (category == null)
            return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("RecordNotFound"), 400);

        var nameArExists = await _unitOfWork.Repository<CardTemplate>()
            .GetQueryable()
            .AnyAsync(t => t.NameAr == request.NameAr);
        if (nameArExists)
            return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);

        var nameEnExists = await _unitOfWork.Repository<CardTemplate>()
            .GetQueryable()
            .AnyAsync(t => t.NameEn == request.NameEn);
        if (nameEnExists)
            return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);

        var entity = _mapper.Map<CardTemplate>(request);
        await _unitOfWork.Repository<CardTemplate>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<CardTemplateAdminDto>(entity);
        return ServiceResult<CardTemplateAdminDto>.Success(dto, _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<CardTemplateAdminDto>> UpdateAsync(Guid id, UpdateCardTemplateRequest request)
    {
        var entity = await _unitOfWork.Repository<CardTemplate>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<CardTemplateAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        if (request.CategoryId.HasValue)
        {
            var category = await _unitOfWork.Repository<TemplateCategory>().GetByIdAsync(request.CategoryId.Value);
            if (category == null)
                return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("RecordNotFound"), 400);
        }

        if (!string.IsNullOrWhiteSpace(request.NameAr))
        {
            var nameArExists = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .AnyAsync(t => t.NameAr == request.NameAr && t.Id != id);
            if (nameArExists)
                return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        if (!string.IsNullOrWhiteSpace(request.NameEn))
        {
            var nameEnExists = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .AnyAsync(t => t.NameEn == request.NameEn && t.Id != id);
            if (nameEnExists)
                return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

        _mapper.Map(request, entity);
        _unitOfWork.Repository<CardTemplate>().Update(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<CardTemplateAdminDto>(entity);
        return ServiceResult<CardTemplateAdminDto>.Success(dto, _messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<CardTemplate>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<bool>.NotFound(_messageService.Get("RecordNotFound"));

        _unitOfWork.Repository<CardTemplate>().Remove(entity);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<bool>.Success(true, _messageService.Get("RecordDeleted"));
    }
}
