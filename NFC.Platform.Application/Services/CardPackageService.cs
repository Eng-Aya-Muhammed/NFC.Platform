using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardPackage;
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

public class CardPackageService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ExportBuilder? exportBuilder = null,
    IExcelExportService? excelExportService = null,
    IPdfExportService? pdfExportService = null) : ICardPackageService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ExportBuilder? _exportBuilder = exportBuilder;
    private readonly IExcelExportService? _excelExportService = excelExportService;
    private readonly IPdfExportService? _pdfExportService = pdfExportService;

    public async Task<ServiceResult<IReadOnlyList<CardPackageDto>>> GetActiveCardPackagesAsync()
    {
        var entities = await _unitOfWork.Repository<CardPackage>()
            .GetQueryable()
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.NumberOfCards)
            .ToListAsync();

        var dtos = _mapper.Map<IReadOnlyList<CardPackageDto>>(entities);
        return ServiceResult<IReadOnlyList<CardPackageDto>>.Success(dtos);
    }

    public async Task<ServiceResult<PagedResult<CardPackageAdminDto>>> GetAllAdminCardPackagesAsync(PaginationRequest request)
    {
        var query = _unitOfWork.Repository<CardPackage>()
            .GetQueryable()
            .AsNoTracking()
            .OrderBy(p => p.NumberOfCards);

        var pagedResult = await query.ToPagedResultAsync(request, p => _mapper.Map<CardPackageAdminDto>(p));
        return ServiceResult<PagedResult<CardPackageAdminDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<byte[]>> ExportCardPackagesAsync(ExportFormat format)
    {
        if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
        {
            return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
        }

        var packages = await _unitOfWork.Repository<CardPackage>()
            .GetQueryable()
            .AsNoTracking()
            .OrderBy(p => p.NumberOfCards)
            .ToListAsync();

        var exportDtos = _mapper.Map<List<CardPackageExportDto>>(packages);
        var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_CardPackages");

        byte[] fileBytes = format switch
        {
            ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
            ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return ServiceResult<byte[]>.Success(fileBytes);
    }

    public async Task<ServiceResult<CardPackageAdminDto>> GetByIdAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<CardPackage>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<CardPackageAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<CardPackageAdminDto>(entity);
        return ServiceResult<CardPackageAdminDto>.Success(dto);
    }

    public async Task<ServiceResult<CardPackageAdminDto>> CreateAsync(CreateCardPackageRequest request)
    {
        var countExists = await _unitOfWork.Repository<CardPackage>()
            .GetQueryable()
            .AnyAsync(p => p.NumberOfCards == request.NumberOfCards);
        if (countExists)
            return ServiceResult<CardPackageAdminDto>.Fail(_messageService.Get("DuplicatePackageNumberOfCards"), 400);

        var entity = _mapper.Map<CardPackage>(request);
        await _unitOfWork.Repository<CardPackage>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<CardPackageAdminDto>(entity);
        return ServiceResult<CardPackageAdminDto>.Success(dto, _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<CardPackageAdminDto>> UpdateAsync(Guid id, UpdateCardPackageRequest request)
    {
        var entity = await _unitOfWork.Repository<CardPackage>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<CardPackageAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        if (request.NumberOfCards.HasValue)
        {
            var countExists = await _unitOfWork.Repository<CardPackage>()
                .GetQueryable()
                .AnyAsync(p => p.NumberOfCards == request.NumberOfCards.Value && p.Id != id);
            if (countExists)
                return ServiceResult<CardPackageAdminDto>.Fail(_messageService.Get("DuplicatePackageNumberOfCards"), 400);
        }

        _mapper.Map(request, entity);
        _unitOfWork.Repository<CardPackage>().Update(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<CardPackageAdminDto>(entity);
        return ServiceResult<CardPackageAdminDto>.Success(dto, _messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<CardPackage>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<bool>.NotFound(_messageService.Get("RecordNotFound"));

        _unitOfWork.Repository<CardPackage>().Remove(entity);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<bool>.Success(true, _messageService.Get("RecordDeleted"));
    }
}
