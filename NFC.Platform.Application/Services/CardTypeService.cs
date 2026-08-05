using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Services;

public class CardTypeService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ExportBuilder? exportBuilder = null,
    IExcelExportService? excelExportService = null,
    IPdfExportService? pdfExportService = null) : ICardTypeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ExportBuilder? _exportBuilder = exportBuilder;
    private readonly IExcelExportService? _excelExportService = excelExportService;
    private readonly IPdfExportService? _pdfExportService = pdfExportService;

    public async Task<ServiceResult<IReadOnlyList<CardTypeDto>>> GetActiveCardTypesAsync(string? search = null)
    {
        var query = _unitOfWork.Repository<CardType>()
            .GetQueryable()
            .AsNoTracking()
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(t => (t.NameAr != null && t.NameAr.Contains(search)) ||
                                     (t.NameEn != null && t.NameEn.Contains(search)));
        }

        var entities = await query.ToListAsync();
        var dtos = _mapper.Map<IReadOnlyList<CardTypeDto>>(entities);
        return ServiceResult<IReadOnlyList<CardTypeDto>>.Success(dtos);
    }

    public async Task<ServiceResult<PagedResult<CardTypeAdminDto>>> GetAllAdminCardTypesAsync(PaginationRequest request, string? search = null)
    {
        var query = _unitOfWork.Repository<CardType>()
            .GetQueryable()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(t => (t.NameAr != null && t.NameAr.Contains(search)) ||
                                     (t.NameEn != null && t.NameEn.Contains(search)));
        }

        query = query.OrderByDescending(t => t.CreatedAt);

        var pagedResult = await query.ToPagedResultAsync(request, t => _mapper.Map<CardTypeAdminDto>(t));
        return ServiceResult<PagedResult<CardTypeAdminDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<byte[]>> ExportCardTypesAsync(ExportFormat format, string? search = null)
    {
        if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
        {
            return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
        }

        var query = _unitOfWork.Repository<CardType>()
            .GetQueryable()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(t => (t.NameAr != null && t.NameAr.Contains(search)) ||
                                     (t.NameEn != null && t.NameEn.Contains(search)));
        }

        var cardTypes = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var exportDtos = _mapper.Map<List<CardTypeExportDto>>(cardTypes);
        var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_CardTypes");

        byte[] fileBytes = format switch
        {
            ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
            ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return ServiceResult<byte[]>.Success(fileBytes);
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
        var trimmedAr = request.NameAr?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedAr))
        {
            var nameArExists = await _unitOfWork.Repository<CardType>()
                .GetQueryable()
                .AnyAsync(t => t.NameAr.Trim() == trimmedAr);
            if (nameArExists)
                return ServiceResult<CardTypeAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        var trimmedEn = request.NameEn?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedEn))
        {
            var nameEnExists = await _unitOfWork.Repository<CardType>()
                .GetQueryable()
                .AnyAsync(t => t.NameEn.Trim() == trimmedEn);
            if (nameEnExists)
                return ServiceResult<CardTypeAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

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
            var trimmedAr = request.NameAr.Trim();
            var nameArExists = await _unitOfWork.Repository<CardType>()
                .GetQueryable()
                .AnyAsync(t => t.NameAr.Trim() == trimmedAr && t.Id != id);
            if (nameArExists)
                return ServiceResult<CardTypeAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        if (!string.IsNullOrWhiteSpace(request.NameEn))
        {
            var trimmedEn = request.NameEn.Trim();
            var nameEnExists = await _unitOfWork.Repository<CardType>()
                .GetQueryable()
                .AnyAsync(t => t.NameEn.Trim() == trimmedEn && t.Id != id);
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
