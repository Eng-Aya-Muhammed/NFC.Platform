using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Services;

public class TemplateCategoryService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ExportBuilder? exportBuilder = null,
    IExcelExportService? excelExportService = null,
    IPdfExportService? pdfExportService = null) : ITemplateCategoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ExportBuilder? _exportBuilder = exportBuilder;
    private readonly IExcelExportService? _excelExportService = excelExportService;
    private readonly IPdfExportService? _pdfExportService = pdfExportService;

    public async Task<ServiceResult<IReadOnlyList<TemplateCategoryDto>>> GetActiveCategoriesAsync()
    {
        var entities = await _unitOfWork.Repository<TemplateCategory>()
            .GetQueryable()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var dtos = _mapper.Map<IReadOnlyList<TemplateCategoryDto>>(entities);
        return ServiceResult<IReadOnlyList<TemplateCategoryDto>>.Success(dtos);
    }

    public async Task<ServiceResult<PagedResult<TemplateCategoryAdminDto>>> GetAllAdminCategoriesAsync(PaginationRequest request, string? search = null)
    {
        var query = _unitOfWork.Repository<TemplateCategory>()
            .GetQueryable()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.NameAr.Contains(search) || c.NameEn.Contains(search));
        }

        query = query.OrderBy(c => c.DisplayOrder);

        var pagedResult = await query.ToPagedResultAsync(request, c => _mapper.Map<TemplateCategoryAdminDto>(c));
        return ServiceResult<PagedResult<TemplateCategoryAdminDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<byte[]>> ExportTemplateCategoriesAsync(ExportFormat format, string? search = null)
    {
        if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
        {
            return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
        }

        var query = _unitOfWork.Repository<TemplateCategory>()
            .GetQueryable()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.NameAr.Contains(search) || c.NameEn.Contains(search));
        }

        var categories = await query
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var exportDtos = _mapper.Map<List<TemplateCategoryExportDto>>(categories);
        var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_TemplateCategories");

        byte[] fileBytes = format switch
        {
            ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
            ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return ServiceResult<byte[]>.Success(fileBytes);
    }

    public async Task<ServiceResult<TemplateCategoryAdminDto>> GetByIdAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<TemplateCategory>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<TemplateCategoryAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<TemplateCategoryAdminDto>(entity);
        return ServiceResult<TemplateCategoryAdminDto>.Success(dto);
    }

    public async Task<ServiceResult<TemplateCategoryAdminDto>> CreateAsync(CreateTemplateCategoryRequest request)
    {
        var trimmedAr = request.NameAr?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedAr))
        {
            var nameArExists = await _unitOfWork.Repository<TemplateCategory>()
                .GetQueryable()
                .AnyAsync(c => c.NameAr.Trim() == trimmedAr);
            if (nameArExists)
                return ServiceResult<TemplateCategoryAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        var trimmedEn = request.NameEn?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedEn))
        {
            var nameEnExists = await _unitOfWork.Repository<TemplateCategory>()
                .GetQueryable()
                .AnyAsync(c => c.NameEn.Trim() == trimmedEn);
            if (nameEnExists)
                return ServiceResult<TemplateCategoryAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

        var entity = _mapper.Map<TemplateCategory>(request);
        await _unitOfWork.Repository<TemplateCategory>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<TemplateCategoryAdminDto>(entity);
        return ServiceResult<TemplateCategoryAdminDto>.Success(dto, _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<TemplateCategoryAdminDto>> UpdateAsync(Guid id, UpdateTemplateCategoryRequest request)
    {
        var entity = await _unitOfWork.Repository<TemplateCategory>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<TemplateCategoryAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        if (!string.IsNullOrWhiteSpace(request.NameAr))
        {
            var trimmedAr = request.NameAr.Trim();
            var nameArExists = await _unitOfWork.Repository<TemplateCategory>()
                .GetQueryable()
                .AnyAsync(c => c.NameAr.Trim() == trimmedAr && c.Id != id);
            if (nameArExists)
                return ServiceResult<TemplateCategoryAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        if (!string.IsNullOrWhiteSpace(request.NameEn))
        {
            var trimmedEn = request.NameEn.Trim();
            var nameEnExists = await _unitOfWork.Repository<TemplateCategory>()
                .GetQueryable()
                .AnyAsync(c => c.NameEn.Trim() == trimmedEn && c.Id != id);
            if (nameEnExists)
                return ServiceResult<TemplateCategoryAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

        _mapper.Map(request, entity);
        _unitOfWork.Repository<TemplateCategory>().Update(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<TemplateCategoryAdminDto>(entity);
        return ServiceResult<TemplateCategoryAdminDto>.Success(dto, _messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<TemplateCategory>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<bool>.NotFound(_messageService.Get("RecordNotFound"));

        _unitOfWork.Repository<TemplateCategory>().Remove(entity);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<bool>.Success(true, _messageService.Get("RecordDeleted"));
    }
}
