using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.DiscountCode;
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

public class DiscountCodeService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ExportBuilder? exportBuilder = null,
    IExcelExportService? excelExportService = null,
    IPdfExportService? pdfExportService = null) : IDiscountCodeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ExportBuilder? _exportBuilder = exportBuilder;
    private readonly IExcelExportService? _excelExportService = excelExportService;
    private readonly IPdfExportService? _pdfExportService = pdfExportService;

    public async Task<ServiceResult<PagedResult<DiscountCodeDto>>> GetPagedAdminAsync(
        PaginationRequest request, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<DiscountCode>()
            .GetQueryable()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.Code.Contains(search));
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var pagedResult = await query.ToPagedResultAsync(request, c => _mapper.Map<DiscountCodeDto>(c), cancellationToken);
        return ServiceResult<PagedResult<DiscountCodeDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<byte[]>> ExportDiscountCodesAsync(ExportFormat format, string? search = null, CancellationToken cancellationToken = default)
    {
        if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
        {
            return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
        }

        var query = _unitOfWork.Repository<DiscountCode>()
            .GetQueryable()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.Code.Contains(search));
        }

        var codes = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var exportDtos = _mapper.Map<List<DiscountCodeExportDto>>(codes);
        var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_DiscountCodes");

        byte[] fileBytes = format switch
        {
            ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
            ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return ServiceResult<byte[]>.Success(fileBytes);
    }

    public async Task<ServiceResult<DiscountCodeDto>> GetByIdAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<DiscountCode>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity == null)
            return ServiceResult<DiscountCodeDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<DiscountCodeDto>(entity);
        return ServiceResult<DiscountCodeDto>.Success(dto);
    }

    public async Task<ServiceResult<DiscountCodeDto>> CreateAsync(CreateDiscountCodeRequest request)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _unitOfWork.Repository<DiscountCode>()
            .GetQueryable()
            .AnyAsync(c => c.Code == normalizedCode);

        if (exists)
            return ServiceResult<DiscountCodeDto>.Fail(_messageService.Get("DuplicateDiscountCode"), 400);

        var entity = _mapper.Map<DiscountCode>(request);
        entity.Code = normalizedCode;

        await _unitOfWork.Repository<DiscountCode>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<DiscountCodeDto>(entity);
        return ServiceResult<DiscountCodeDto>.Success(dto, _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<DiscountCodeDto>> UpdateAsync(Guid id, UpdateDiscountCodeRequest request)
    {
        var entity = await _unitOfWork.Repository<DiscountCode>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<DiscountCodeDto>.NotFound(_messageService.Get("RecordNotFound"));

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            var exists = await _unitOfWork.Repository<DiscountCode>()
                .GetQueryable()
                .AnyAsync(c => c.Code == normalizedCode && c.Id != id);

            if (exists)
                return ServiceResult<DiscountCodeDto>.Fail(_messageService.Get("DuplicateDiscountCode"), 400);
        }

        _mapper.Map(request, entity);
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            entity.Code = request.Code.Trim().ToUpperInvariant();
        }

        _unitOfWork.Repository<DiscountCode>().Update(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<DiscountCodeDto>(entity);
        return ServiceResult<DiscountCodeDto>.Success(dto, _messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        var entity = await _unitOfWork.Repository<DiscountCode>().GetByIdAsync(id);
        if (entity == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        _unitOfWork.Repository<DiscountCode>().Remove(entity);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordDeleted"));
    }

    public async Task<ServiceResult<DiscountCodeValidationResultDto>> ValidateCodeAsync(ValidateDiscountCodeRequest request)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var entity = await _unitOfWork.Repository<DiscountCode>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == normalizedCode);

        if (entity == null)
        {
            return ServiceResult<DiscountCodeValidationResultDto>.Success(new DiscountCodeValidationResultDto
            {
                IsValid = false,
                Code = request.Code,
                DiscountValue = 0,
                CalculatedDiscountAmount = 0,
                FinalAmount = request.OrderAmount,
                ErrorMessage = _messageService.Get("RecordNotFound")
            });
        }

        var now = DateTime.UtcNow;

        if (now < entity.StartDate)
        {
            return ServiceResult<DiscountCodeValidationResultDto>.Success(new DiscountCodeValidationResultDto
            {
                IsValid = false,
                Code = entity.Code,
                DiscountValue = entity.DiscountValue,
                CalculatedDiscountAmount = 0,
                FinalAmount = request.OrderAmount,
                ErrorMessage = _messageService.Get("DiscountCodeNotStartedYet")
            });
        }

        if (now > entity.EndDate)
        {
            return ServiceResult<DiscountCodeValidationResultDto>.Success(new DiscountCodeValidationResultDto
            {
                IsValid = false,
                Code = entity.Code,
                DiscountValue = entity.DiscountValue,
                CalculatedDiscountAmount = 0,
                FinalAmount = request.OrderAmount,
                ErrorMessage = _messageService.Get("DiscountCodeExpired")
            });
        }

        var discountAmount = Math.Min(entity.DiscountValue, request.OrderAmount);
        var finalAmount = Math.Max(0, request.OrderAmount - discountAmount);

        return ServiceResult<DiscountCodeValidationResultDto>.Success(new DiscountCodeValidationResultDto
        {
            IsValid = true,
            Code = entity.Code,
            DiscountValue = entity.DiscountValue,
            CalculatedDiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            ErrorMessage = null
        });
    }
}
