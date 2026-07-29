using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NFC.Platform.Application.DTOs.CardDesign;
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

public class CardDesignService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ICurrentTenant currentTenant,
    IEmployeeService employeeService,
    IConfiguration configuration) : ICardDesignService
{
    private readonly IUnitOfWork _unitOfWork       = unitOfWork      ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper               = mapper          ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ICurrentTenant _currentTenant = currentTenant   ?? throw new ArgumentNullException(nameof(currentTenant));
    private readonly IEmployeeService _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
    private readonly IConfiguration _configuration = configuration   ?? throw new ArgumentNullException(nameof(configuration));

    // ─────────────────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<CardDesignDto>> GetDesignByIdAsync(Guid id)
    {
        var design = await _unitOfWork.Repository<CardDesign>()
            .GetQueryable()
            .AsNoTracking()
            .Include(d => d.CardType)
            .Include(d => d.CardPackage)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (design == null)
            return ServiceResult<CardDesignDto>.NotFound(_messageService.Get("DesignNotFound"));

        return ServiceResult<CardDesignDto>.Success(_mapper.Map<CardDesignDto>(design));
    }

    public async Task<ServiceResult<PagedResult<CardDesignDto>>> GetPagedDesignsAsync(PaginationRequest request)
    {
        var query = _unitOfWork.Repository<CardDesign>()
            .GetQueryable()
            .AsNoTracking()
            .Include(d => d.CardType)
            .OrderByDescending(d => d.CreatedAt);

        var paged = await query.ToPagedResultAsync(request, d => _mapper.Map<CardDesignDto>(d));
        return ServiceResult<PagedResult<CardDesignDto>>.Success(paged);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<CardDesignDto>> CreateDesignAsync(CreateCardDesignRequest request)
    {
        var userId = _currentTenant.UserId;
        if (!userId.HasValue)
            return ServiceResult<CardDesignDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return ServiceResult<CardDesignDto>.Fail(_messageService.Get("InvalidTenantClaim"), 400);

        // Load AccountType (lightweight projection)
        var accountType = await _unitOfWork.Repository<User>()
            .GetQueryable().AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => (AccountType?)u.AccountType)
            .FirstOrDefaultAsync();

        if (accountType == null)
            return ServiceResult<CardDesignDto>.NotFound(_messageService.Get("UserNotAuthenticated"));

        var isCompany = accountType == AccountType.CompanyAdmin;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Validate CardType if provided
            if (request.CardTypeId != Guid.Empty)
            {
                var cardType = await _unitOfWork.Repository<CardType>().GetByIdAsync(request.CardTypeId);
                if (cardType == null || !cardType.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardDesignDto>.Fail(_messageService.Get("InvalidOrInactiveCardType"), 400);
                }
            }

            Guid   resolvedPackageId;
            int    totalQuantity;
            decimal unitPrice;
            decimal totalPrice;

            if (isCompany)
            {
                // ── Company: CustomQuantity required; pricing via unit-package ──────
                if (!request.CustomQuantity.HasValue || request.CustomQuantity <= 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardDesignDto>.Fail(_messageService.Get("CustomQuantityRequired"), 422);
                }

                // Find unit-price package (NumberOfCards = 1)
                var unitPackage = await _unitOfWork.Repository<CardPackage>()
                    .GetQueryable().AsNoTracking()
                    .Where(p => p.NumberOfCards == 1 && p.IsActive)
                    .Select(p => new { p.Id, p.Price })
                    .FirstOrDefaultAsync();

                if (unitPackage == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardDesignDto>.Fail(_messageService.Get("UnitCardPackageNotFound"), 500);
                }

                resolvedPackageId = unitPackage.Id;
                totalQuantity     = request.CustomQuantity.Value;
                unitPrice         = unitPackage.Price;
                totalPrice        = unitPrice * totalQuantity;

                // Optionally upsert employees from Excel (data only — no card count here)
                if (!string.IsNullOrWhiteSpace(request.ExcelDataUrl))
                {
                    var company = await _unitOfWork.Repository<Company>()
                        .GetQueryable().AsNoTracking()
                        .Where(c => c.TenantId == tenantId.Value)
                        .Select(c => new { c.Id })
                        .FirstOrDefaultAsync();

                    if (company == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<CardDesignDto>.Fail(_messageService.Get("CompanyNotFound"), 400);
                    }

                    var excelResult = await _employeeService.UpsertEmployeesFromExcelAsync(
                        request.ExcelDataUrl, company.Id, tenantId.Value);

                    if (!excelResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<CardDesignDto>.Fail(
                            excelResult.Message ?? string.Join(", ", excelResult.Errors),
                            excelResult.StatusCode);
                    }
                }
            }
            else
            {
                // ── Individual: CardPackageId required; pricing from package ─────────
                if (!request.CardPackageId.HasValue || request.CardPackageId == Guid.Empty)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardDesignDto>.Fail(_messageService.Get("CardPackageIdRequired"), 422);
                }

                var package = await _unitOfWork.Repository<CardPackage>()
                    .GetByIdAsync(request.CardPackageId.Value);

                if (package == null || !package.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardDesignDto>.Fail(_messageService.Get("InvalidOrInactiveCardPackage"), 400);
                }

                resolvedPackageId = package.Id;
                totalQuantity     = package.NumberOfCards;
                unitPrice         = package.NumberOfCards > 0 ? package.Price / package.NumberOfCards : package.Price;
                totalPrice        = package.Price;
            }

            // ── Map & set computed/server-owned fields ──────────────────────────
            var design = _mapper.Map<CardDesign>(request);
            design.TenantId             = tenantId.Value;
            design.UserId               = userId.Value;
            design.CardTypeId           = request.CardTypeId;
            design.CardPackageId        = resolvedPackageId;
            design.TotalQuantity        = totalQuantity;
            design.UsedQuantity         = 0;
            design.UnitPrice            = unitPrice;
            design.TotalPrice           = totalPrice;
            design.Currency             = "KWD";
            design.IsPaid               = false;
            design.PaymentStatus        = CardDesignPaymentStatus.Pending;

            await _unitOfWork.Repository<CardDesign>().AddAsync(design);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Reload for response DTO
            var created = await _unitOfWork.Repository<CardDesign>()
                .GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == design.Id);

            return ServiceResult<CardDesignDto>.Success(
                _mapper.Map<CardDesignDto>(created ?? design),
                _messageService.Get("RecordCreated"));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<ServiceResult<string>> GetPaymentUrlAsync(Guid designId)
    {
        // Load only the fields needed (lightweight projection)
        var design = await _unitOfWork.Repository<CardDesign>()
            .GetQueryable().AsNoTracking()
            .Where(d => d.Id == designId)
            .Select(d => new { d.Id, d.IsPaid, d.TotalPrice, d.Currency })
            .FirstOrDefaultAsync();

        if (design == null)
            return ServiceResult<string>.NotFound(_messageService.Get("DesignNotFound"));

        if (design.IsPaid)
            return ServiceResult<string>.Fail(_messageService.Get("DesignAlreadyPaid"), 400);

        // TODO: Integrate with actual payment gateway (MyFatoorah / Tap / KNet / other)
        // The URL should include the designId as a reference so the callback can be matched.
        var gatewayBaseUrl = _configuration["PaymentGateway:BaseUrl"] ?? "https://payment-gateway.example.com/pay";
        var callbackBase   = _configuration["PaymentGateway:CallbackBase"] ?? "https://api.yourapp.com";

        var paymentUrl = $"{gatewayBaseUrl}?amount={design.TotalPrice:F3}&currency={design.Currency}" +
                         $"&reference={design.Id}&callback={callbackBase}/api/card-designs/{design.Id}/payment-callback";

        return ServiceResult<string>.Success(paymentUrl);
    }

    public async Task<ServiceResult> HandlePaymentCallbackAsync(Guid designId, PaymentCallbackRequest request)
    {
        // 1. Verify HMAC-SHA256 signature from the gateway
        var secret = _configuration["PaymentGateway:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            var expectedSig = ComputeHmacSha256(
                $"{designId}:{request.TransactionId}:{request.IsSuccess}", secret);

            if (!string.Equals(expectedSig, request.GatewaySignature,
                    StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail(_messageService.Get("InvalidPaymentSignature"), 403);
            }
        }

        // 2. Load design for update (with tracking)
        var design = await _unitOfWork.Repository<CardDesign>()
            .GetQueryable()
            .FirstOrDefaultAsync(d => d.Id == designId);

        if (design == null)
            return ServiceResult.NotFound(_messageService.Get("DesignNotFound"));

        if (design.IsPaid)
            return ServiceResult.Fail(_messageService.Get("DesignAlreadyPaid"), 400);

        // 3. Apply payment outcome
        if (request.IsSuccess)
        {
            design.IsPaid                = true;
            design.PaymentStatus         = CardDesignPaymentStatus.Paid;
            design.PaidAt                = DateTime.UtcNow;
            design.PaymentTransactionId  = request.TransactionId;
        }
        else
        {
            design.PaymentStatus = CardDesignPaymentStatus.Failed;
        }

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private static string ComputeHmacSha256(string payload, string secret)
    {
        var keyBytes     = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac   = new HMACSHA256(keyBytes);
        var hash         = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
