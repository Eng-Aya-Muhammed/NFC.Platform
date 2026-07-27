using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Localization;


namespace NFC.Platform.Application.Services;

public class CardTypeService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService) : ICardTypeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

    public async Task<ServiceResult<IReadOnlyList<CardTypeDto>>> GetActiveCardTypesAsync()
    {
        var entities = await _unitOfWork.Repository<CardType>()
            .GetQueryable()
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync();

        var dtos = _mapper.Map<IReadOnlyList<CardTypeDto>>(entities);
        return ServiceResult<IReadOnlyList<CardTypeDto>>.Success(dtos);
    }

    public async Task<ServiceResult<PagedResult<CardTypeAdminDto>>> GetAllAdminCardTypesAsync(PaginationRequest request)
    {
        var query = _unitOfWork.Repository<CardType>()
            .GetQueryable()
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt);

        var pagedResult = await query.ToPagedResultAsync(request, t => _mapper.Map<CardTypeAdminDto>(t));
        return ServiceResult<PagedResult<CardTypeAdminDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<CardTypeAdminDto>> GetByIdAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<CardType>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<CardTypeAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<CardTypeAdminDto>(entity);
        return ServiceResult<CardTypeAdminDto>.Success(dto);
    }

    public async Task<ServiceResult<CardTypeAdminDto>> CreateAsync(CreateCardTypeRequest request)
    {
        var nameArExists = await _unitOfWork.Repository<CardType>()
            .GetQueryable()
            .AnyAsync(t => t.NameAr == request.NameAr);
        if (nameArExists)
            return ServiceResult<CardTypeAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);

        var nameEnExists = await _unitOfWork.Repository<CardType>()
            .GetQueryable()
            .AnyAsync(t => t.NameEn == request.NameEn);
        if (nameEnExists)
            return ServiceResult<CardTypeAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);

        var entity = _mapper.Map<CardType>(request);
        await _unitOfWork.Repository<CardType>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<CardTypeAdminDto>(entity);
        return ServiceResult<CardTypeAdminDto>.Success(dto, _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<CardTypeAdminDto>> UpdateAsync(Guid id, UpdateCardTypeRequest request)
    {
        var entity = await _unitOfWork.Repository<CardType>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<CardTypeAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        if (!string.IsNullOrWhiteSpace(request.NameAr))
        {
            var nameArExists = await _unitOfWork.Repository<CardType>()
                .GetQueryable()
                .AnyAsync(t => t.NameAr == request.NameAr && t.Id != id);
            if (nameArExists)
                return ServiceResult<CardTypeAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        if (!string.IsNullOrWhiteSpace(request.NameEn))
        {
            var nameEnExists = await _unitOfWork.Repository<CardType>()
                .GetQueryable()
                .AnyAsync(t => t.NameEn == request.NameEn && t.Id != id);
            if (nameEnExists)
                return ServiceResult<CardTypeAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

        _mapper.Map(request, entity);
        _unitOfWork.Repository<CardType>().Update(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<CardTypeAdminDto>(entity);
        return ServiceResult<CardTypeAdminDto>.Success(dto, _messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<CardType>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<bool>.NotFound(_messageService.Get("RecordNotFound"));

        _unitOfWork.Repository<CardType>().Remove(entity);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<bool>.Success(true, _messageService.Get("RecordDeleted"));
    }
}
