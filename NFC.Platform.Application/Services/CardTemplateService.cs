using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardTemplate;
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

public class CardTemplateService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ExportBuilder? exportBuilder = null,
    IExcelExportService? excelExportService = null,
    IPdfExportService? pdfExportService = null) : ICardTemplateService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ExportBuilder? _exportBuilder = exportBuilder;
    private readonly IExcelExportService? _excelExportService = excelExportService;
    private readonly IPdfExportService? _pdfExportService = pdfExportService;

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

    public async Task<ServiceResult<byte[]>> ExportCardTemplatesAsync(ExportFormat format)
    {
        if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
        {
            return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
        }

        var templates = await _unitOfWork.Repository<CardTemplate>()
            .GetQueryable()
            .AsNoTracking()
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();

        var exportDtos = _mapper.Map<List<CardTemplateExportDto>>(templates);
        var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_CardTemplates");

        byte[] fileBytes = format switch
        {
            ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
            ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return ServiceResult<byte[]>.Success(fileBytes);
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

        var trimmedAr = request.NameAr?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedAr))
        {
            var nameArExists = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .AnyAsync(t => t.NameAr.Trim() == trimmedAr);
            if (nameArExists)
                return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        var trimmedEn = request.NameEn?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedEn))
        {
            var nameEnExists = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .AnyAsync(t => t.NameEn.Trim() == trimmedEn);
            if (nameEnExists)
                return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

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
            var trimmedAr = request.NameAr.Trim();
            var nameArExists = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .AnyAsync(t => t.NameAr.Trim() == trimmedAr && t.Id != id);
            if (nameArExists)
                return ServiceResult<CardTemplateAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        if (!string.IsNullOrWhiteSpace(request.NameEn))
        {
            var trimmedEn = request.NameEn.Trim();
            var nameEnExists = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .AnyAsync(t => t.NameEn.Trim() == trimmedEn && t.Id != id);
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
