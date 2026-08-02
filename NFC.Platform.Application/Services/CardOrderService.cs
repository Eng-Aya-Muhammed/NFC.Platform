using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.DTOs.Settings;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Services;

public class CardOrderService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ICurrentTenant currentTenant,
    IValidator<CreateCardOrderRequest> validator,
    IValidator<UpdateCardOrderRequest> updateValidator,
    IBackgroundJobClient backgroundJobClient,
    IEmployeeService employeeService,
    IOptions<OtpSettings> otpSettings,
    ExportBuilder? exportBuilder = null,
    IExcelExportService? excelExportService = null,
    IPdfExportService? pdfExportService = null) : ICardOrderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
    private readonly IValidator<CreateCardOrderRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IValidator<UpdateCardOrderRequest> _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
    private readonly IEmployeeService _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
    private readonly OtpSettings _otpSettings = otpSettings?.Value ?? throw new ArgumentNullException(nameof(otpSettings));
    private readonly ExportBuilder? _exportBuilder = exportBuilder;
    private readonly IExcelExportService? _excelExportService = excelExportService;
    private readonly IPdfExportService? _pdfExportService = pdfExportService;

    // ─────────────────────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedOrdersAsync(PaginationRequest request, string? statusFilter)
    {
        var query = _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardType)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardPackage)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter)
            && Enum.TryParse<OrderStatus>(statusFilter, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        var pagedResult = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToPagedResultAsync(request, o => _mapper.Map<CardOrderDto>(o));

        return ServiceResult<PagedResult<CardOrderDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<byte[]>> ExportOrdersAsync(ExportFormat format, string? statusFilter)
    {
        if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
        {
            return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
        }

        var query = _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardType)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardPackage)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter)
            && Enum.TryParse<OrderStatus>(statusFilter, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var exportDtos = _mapper.Map<List<CardOrderExportDto>>(orders);
        var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_CardOrders");

        byte[] fileBytes = format switch
        {
            ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
            ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return ServiceResult<byte[]>.Success(fileBytes);
    }

    public async Task<ServiceResult<CardOrderDto>> GetOrderByIdAsync(Guid id)
    {
        var order = await GetOrderWithItemsAsync(id);

        if (order == null)
            return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

        return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(order));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<CardOrderDto>> CreateOrderAsync(CreateCardOrderRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ServiceResult<CardOrderDto>.Fail(validationResult.Errors.Select(e => e.ErrorMessage).ToList(), 422);

        var userId = _currentTenant.UserId;
        if (!userId.HasValue)
            return ServiceResult<CardOrderDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return ServiceResult<CardOrderDto>.Fail(_messageService.Get("InvalidTenantClaim"), 400);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // 1. Determine AccountType & calculate quantity/build items
            var calculationResult = await CalculateOrderQuantityAndBuildItemsAsync(
                userId.Value,
                request.AssignmentScope,
                request.EmployeeIds,
                request.QuantityPerEmployee,
                request.Quantity);

            if (!calculationResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<CardOrderDto>.Fail(calculationResult.Message!, calculationResult.StatusCode);
            }

            var (totalCards, itemsToOrder) = calculationResult.Data;

            // 2. Auto-resolve & validate CardDesign
            var designResult = await ResolveAndValidateCardDesignAsync(
                request.CardDesignId,
                tenantId.Value,
                totalCards);

            if (!designResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<CardOrderDto>.Fail(designResult.Message!, designResult.StatusCode);
            }

            var designData = designResult.Data!;

            // 3. Build & persist order (inherits CardDesign)
            var order = _mapper.Map<CardOrder>(request) ?? new CardOrder();
            order.UserId               = userId.Value;
            order.TenantId             = tenantId.Value;
            order.CardDesignId         = designData.Id;
            order.UnitPrice            = designData.UnitPrice;
            order.TotalPrice           = designData.TotalPrice;
            order.Currency             = designData.Currency;
            order.Quantity             = totalCards;
            order.QuantityPerEmployee  = request.QuantityPerEmployee ?? 1;
            order.Status               = OrderStatus.PendingReview;

            if (itemsToOrder.Count > 0)
                order.Items = itemsToOrder;

            await _unitOfWork.Repository<CardOrder>().AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var created = await GetOrderWithItemsAsync(order.Id);
            return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(created), _messageService.Get("RecordCreated"));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<ServiceResult<CardOrderDto>> CreateReorderAsync(Guid parentOrderId, ReorderRequest request)
    {
        var userId = _currentTenant.UserId;
        if (!userId.HasValue)
            return ServiceResult<CardOrderDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return ServiceResult<CardOrderDto>.Fail(_messageService.Get("InvalidTenantClaim"), 400);

        var parentOrder = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable().AsNoTracking()
            .Include(o => o.CardDesign)
            .FirstOrDefaultAsync(o => o.Id == parentOrderId);

        if (parentOrder == null)
            return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

        var packageIdToUse = request.CardPackageId ?? (parentOrder.CardDesign != null ? parentOrder.CardDesign.CardPackageId : Guid.Empty);
        var cardPackage = await _unitOfWork.Repository<CardPackage>().GetByIdAsync(packageIdToUse);
        if (cardPackage == null || !cardPackage.IsActive)
            return ServiceResult<CardOrderDto>.Fail(_messageService.Get("InvalidOrInactiveCardPackage"), 400);

        var itemsResult = await BuildOrderItemsAsync(request.AssignmentScope, request.EmployeeIds);
        if (!itemsResult.IsSuccess)
            return ServiceResult<CardOrderDto>.Fail(itemsResult.Message ?? string.Join(", ", itemsResult.Errors), itemsResult.StatusCode);

        var itemsToOrder = itemsResult.Data ?? [];
        var totalRequiredCards = itemsToOrder.Where(i => i.RequiresCard).Sum(i => i.NumberOfCardsRequired);

        if (totalRequiredCards > cardPackage.NumberOfCards)
            return ServiceResult<CardOrderDto>.Fail(_messageService.Get("CardPackageCapacityExceeded", totalRequiredCards.ToString(), cardPackage.NumberOfCards.ToString()), 400);

        var cardsToValidate = totalRequiredCards > 0 ? totalRequiredCards : cardPackage.NumberOfCards;

        // Resolve and validate CardDesign capacity for Reorder
        Guid? designIdToValidate = parentOrder.CardDesignId.HasValue && parentOrder.CardDesignId.Value != Guid.Empty ? parentOrder.CardDesignId : null;

        var designResult = await ResolveAndValidateCardDesignAsync(
            designIdToValidate,
            tenantId.Value,
            cardsToValidate);

        var reorder = BuildReorder(parentOrder, request, userId.Value, cardPackage, itemsToOrder);

        if (designResult.IsSuccess && designResult.Data != null)
        {
            var designData = designResult.Data;
            reorder.CardDesignId = designData.Id;
            reorder.UnitPrice    = designData.UnitPrice;
            reorder.TotalPrice   = designData.TotalPrice;
            reorder.Currency     = designData.Currency;
        }
        else if (designIdToValidate.HasValue)
        {
            // Return failure if an explicit design was set but is unpaid/exceeded capacity
            return ServiceResult<CardOrderDto>.Fail(designResult.Message!, designResult.StatusCode);
        }

        await _unitOfWork.Repository<CardOrder>().AddAsync(reorder);
        await _unitOfWork.SaveChangesAsync();

        var created = await GetOrderWithItemsAsync(reorder.Id);
        return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(created), _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<CardOrderDto>> UpdateOrderAsync(Guid id, UpdateCardOrderRequest request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ServiceResult<CardOrderDto>.Fail(validationResult.Errors.Select(e => e.ErrorMessage).ToList(), 422);

        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return ServiceResult<CardOrderDto>.Fail(_messageService.Get("InvalidTenantClaim"), 400);

        var repo = _unitOfWork.Repository<CardOrder>();
        var order = await repo.GetQueryable()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        if (order == null)
            return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.PendingReview)
            return ServiceResult<CardOrderDto>.Fail(_messageService.Get("OrderCannotBeUpdated"), 400);

        var userId = _currentTenant.UserId;
        if (userId.HasValue)
        {
            var currentUser = await _unitOfWork.Repository<User>().GetQueryable().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (currentUser != null && currentUser.AccountType == AccountType.Individual)
            {
                request.AssignmentScope = AssignmentScope.Individual;
            }
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var currentScope = request.AssignmentScope ?? (order.Items.Count > 0 ? AssignmentScope.SpecificEmployees : AssignmentScope.Individual);

            if ((request.AssignmentScope.HasValue || (request.EmployeeIds != null && request.EmployeeIds.Count > 0) || request.QuantityPerEmployee.HasValue || request.Quantity.HasValue) && order.CardDesignId.HasValue)
            {
                var designData = await _unitOfWork.Repository<CardDesign>()
                    .GetQueryable().AsNoTracking()
                    .Where(d => d.Id == order.CardDesignId.Value)
                    .Select(d => new { d.TotalQuantity, d.UsedQuantity })
                    .FirstOrDefaultAsync();

                if (designData != null)
                {
                    var itemsResult = await BuildOrderItemsAsync(currentScope, request.EmployeeIds);
                    if (!itemsResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<CardOrderDto>.Fail(itemsResult.Message ?? string.Join(", ", itemsResult.Errors), itemsResult.StatusCode);
                    }

                    var newItems = itemsResult.Data ?? new List<CardOrderItem>();
                    var qtyPerEmp = request.QuantityPerEmployee ?? order.QuantityPerEmployee;
                    var totalRequiredCards = newItems.Count > 0 ? newItems.Count * qtyPerEmp : (request.Quantity ?? order.Quantity);

                    var otherPendingQuantity = await CalculatePendingOrdersQuantityAsync(order.CardDesignId.Value, excludeOrderId: order.Id);

                    var availableQty = designData.TotalQuantity - designData.UsedQuantity - otherPendingQuantity;
                    if (totalRequiredCards > availableQty)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<CardOrderDto>.Fail(
                            _messageService.Get("DesignRemainingQuantityExceeded", totalRequiredCards.ToString(), availableQty < 0 ? "0" : availableQty.ToString()), 400);
                    }

                    var oldItems = order.Items.ToList();
                    foreach (var item in oldItems)
                    {
                        _unitOfWork.Repository<CardOrderItem>().Remove(item);
                    }

                    var itemRepo = _unitOfWork.Repository<CardOrderItem>();
                    foreach (var newItem in newItems)
                    {
                        newItem.CardOrderId = order.Id;
                        newItem.TenantId = order.TenantId;
                        newItem.NumberOfCardsRequired = qtyPerEmp;
                        await itemRepo.AddAsync(newItem);
                    }

                    order.Quantity = totalRequiredCards;
                    order.QuantityPerEmployee = qtyPerEmp;
                }
            }

            _mapper.Map(request, order);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var updated = await GetOrderWithItemsAsync(order.Id);
            return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(updated), _messageService.Get("RecordUpdated"));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<ServiceResult> CancelOrderAsync(Guid id)
    {
        var repo = _unitOfWork.Repository<CardOrder>();
        var order = await repo.GetByIdAsync(id);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.PendingReview)
            return ServiceResult.Fail(_messageService.Get("OrderCannotBeCancelled"), 400);

        order.Status = OrderStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult> ResendOrderOtpAsync(Guid orderId)
    {
        var tenantId = _currentTenant.TenantId;
        var order = await _unitOfWork.Repository<CardOrder>().GetQueryable()
            .Include(o => o.Tenant).ThenInclude(t => t.Company).ThenInclude(c => c!.AdminUser).ThenInclude(u => u!.UserProfile)
            .Include(o => o.User).ThenInclude(u => u.UserProfile)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.ReadyForDelivery)
            return ServiceResult.Fail(_messageService.Get("OrderNotReadyForDelivery"), 422);

        if (order.DeliveryOtpLastSentAt.HasValue &&
            (DateTime.UtcNow - order.DeliveryOtpLastSentAt.Value).TotalSeconds < _otpSettings.CooldownSeconds)
            return ServiceResult.Fail(_messageService.Get("OtpCooldownActive"), 422);

        if (order.DeliveryOtpResendCount >= _otpSettings.MaxResendAttempts)
            return ServiceResult.Fail(_messageService.Get("OtpResendLimitReached"), 422);

        var newOtp = Random.Shared.Next(100000, 999999).ToString();
        order.DeliveryOtp = newOtp;
        order.DeliveryOtpExpiresAt = DateTime.UtcNow.AddDays(7);
        order.DeliveryOtpLastSentAt = DateTime.UtcNow;
        order.DeliveryOtpResendCount++;
        order.DeliveryOtpFailedAttempts = 0;
        await _unitOfWork.SaveChangesAsync();

        var recipient = order.Tenant?.Company?.AdminUser ?? order.User;
        if (recipient != null)
            EnqueueOtpNotifications(recipient, newOtp, order.CardDesign?.CardType?.NameAr ?? "Physical Card");

        return ServiceResult.Success(_messageService.Get("OtpResent"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper DTO for Design Resolution
    // ─────────────────────────────────────────────────────────────────────────

    public record ResolvedCardDesignInfo(
        Guid Id,
        Guid CardTypeId,
        bool IsPaid,
        int TotalQuantity,
        int UsedQuantity,
        Guid CardPackageId,
        decimal UnitPrice,
        decimal TotalPrice,
        string Currency);

    // ─────────────────────────────────────────────────────────────────────────
    // Private Helpers & Sub-routines
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<ServiceResult> ValidateCardTypeAsync(Guid cardTypeId)
    {
        if (cardTypeId == Guid.Empty) return ServiceResult.Success();

        var cardType = await _unitOfWork.Repository<CardType>().GetByIdAsync(cardTypeId);
        if (cardType != null && !cardType.IsActive)
        {
            return ServiceResult.Fail(_messageService.Get("InvalidOrInactiveCardType"), 400);
        }
        return ServiceResult.Success();
    }

    private async Task<ServiceResult<(int TotalCards, List<CardOrderItem> ItemsToOrder)>> CalculateOrderQuantityAndBuildItemsAsync(
        Guid userId,
        AssignmentScope? assignmentScope,
        List<Guid>? employeeIds,
        int? quantityPerEmployee,
        int? quantity)
    {
        var currentUser = await _unitOfWork.Repository<User>()
            .GetQueryable().AsNoTracking()
            .Select(u => new { u.Id, u.AccountType })
            .FirstOrDefaultAsync(u => u.Id == userId);

        var isCompany = currentUser?.AccountType == AccountType.CompanyAdmin;

        var itemsToOrder = new List<CardOrderItem>();
        int totalCards;

        if (isCompany)
        {
            if (assignmentScope == null)
            {
                return ServiceResult<(int, List<CardOrderItem>)>.Fail(
                    _messageService.Get("RequiredField", _messageService.Get("AssignmentScope")), 422);
            }

            var qtyPerEmp = quantityPerEmployee ?? 1;

            var itemsResult = await BuildOrderItemsAsync(assignmentScope, employeeIds);
            if (!itemsResult.IsSuccess)
            {
                return ServiceResult<(int, List<CardOrderItem>)>.Fail(
                    itemsResult.Message ?? string.Join(", ", itemsResult.Errors), itemsResult.StatusCode);
            }

            itemsToOrder = itemsResult.Data ?? [];
            totalCards   = itemsToOrder.Count * qtyPerEmp;

            foreach (var item in itemsToOrder)
                item.NumberOfCardsRequired = qtyPerEmp;
        }
        else
        {
            if (!quantity.HasValue || quantity <= 0)
            {
                return ServiceResult<(int, List<CardOrderItem>)>.Fail(
                    _messageService.Get("RequiredField", _messageService.Get("Quantity")), 422);
            }

            totalCards = quantity.Value;
        }

        return ServiceResult<(int, List<CardOrderItem>)>.Success((totalCards, itemsToOrder));
    }

    private async Task<ServiceResult<ResolvedCardDesignInfo>> ResolveAndValidateCardDesignAsync(
        Guid? requestedCardDesignId,
        Guid tenantId,
        int totalCards)
    {
        var baseQuery = _unitOfWork.Repository<CardDesign>()
            .GetQueryable()
            .AsNoTracking();

        IQueryable<CardDesign> designQuery;
        if (requestedCardDesignId.HasValue && requestedCardDesignId.Value != Guid.Empty)
        {
            designQuery = baseQuery.Where(d => d.Id == requestedCardDesignId.Value);
        }
        else
        {
            designQuery = baseQuery.Where(d => d.TenantId == tenantId && d.IsPaid).OrderByDescending(d => d.PaidAt ?? d.CreatedAt);
        }

        var candidateDesignsList = designQuery
            .Select(d => new ResolvedCardDesignInfo(
                d.Id,
                d.CardTypeId,
                d.IsPaid,
                d.TotalQuantity,
                d.UsedQuantity,
                d.CardPackageId,
                d.UnitPrice,
                d.TotalPrice,
                d.Currency));

        List<ResolvedCardDesignInfo> candidateDesigns;
        try
        {
            candidateDesigns = await candidateDesignsList.ToListAsync();
        }
        catch (InvalidOperationException)
        {
            candidateDesigns = candidateDesignsList.ToList();
        }

        if (candidateDesigns.Count == 0)
        {
            if (requestedCardDesignId.HasValue && requestedCardDesignId.Value != Guid.Empty)
            {
                return ServiceResult<ResolvedCardDesignInfo>.NotFound(_messageService.Get("DesignNotFound"));
            }
            return ServiceResult<ResolvedCardDesignInfo>.Fail(_messageService.Get("DesignPaymentRequired"), 402);
        }

        ResolvedCardDesignInfo? selectedDesign = null;
        int selectedAvailableQty = 0;

        foreach (var candidate in candidateDesigns)
        {
            if (!candidate.IsPaid)
            {
                return ServiceResult<ResolvedCardDesignInfo>.Fail(_messageService.Get("DesignPaymentRequired"), 402);
            }

            var pendingQty = await CalculatePendingOrdersQuantityAsync(candidate.Id);

            var avail = candidate.TotalQuantity - candidate.UsedQuantity - pendingQty;
            if (totalCards <= avail)
            {
                selectedDesign = candidate;
                selectedAvailableQty = avail;
                break;
            }
            else if (requestedCardDesignId.HasValue && requestedCardDesignId.Value != Guid.Empty)
            {
                selectedDesign = candidate;
                selectedAvailableQty = avail;
                break;
            }
        }

        if (selectedDesign == null || totalCards > selectedAvailableQty)
        {
            return ServiceResult<ResolvedCardDesignInfo>.Fail(
                _messageService.Get("DesignRemainingQuantityExceeded",
                    totalCards.ToString(), selectedAvailableQty < 0 ? "0" : selectedAvailableQty.ToString()), 400);
        }

        return ServiceResult<ResolvedCardDesignInfo>.Success(selectedDesign);
    }

    private async Task<int> CalculatePendingOrdersQuantityAsync(Guid cardDesignId, Guid? excludeOrderId = null)
    {
        var query = _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Where(o => o.CardDesignId == cardDesignId
                     && (o.Status == OrderStatus.PendingReview || o.Status == OrderStatus.UnderReview || o.Status == OrderStatus.AwaitingDesign));

        if (excludeOrderId.HasValue && excludeOrderId.Value != Guid.Empty)
        {
            query = query.Where(o => o.Id != excludeOrderId.Value);
        }

        try
        {
            return await query.SumAsync(o => (int?)o.Quantity) ?? 0;
        }
        catch (InvalidOperationException)
        {
            return query.Sum(o => (int?)o.Quantity) ?? 0;
        }
    }

    private static CardOrder BuildReorder(CardOrder parent, ReorderRequest request, Guid userId,
        CardPackage package, List<CardOrderItem> items)
    {
        var quantity   = package.NumberOfCards;
        var unitPrice  = package.NumberOfCards > 0 ? package.Price / package.NumberOfCards : package.Price;
        var totalPrice = package.Price;

        return new CardOrder
        {
            UserId          = userId,
            ParentOrderId   = parent.Id,
            CardDesignId    = parent.CardDesignId,
            Quantity        = quantity,
            Notes           = parent.Notes,
            UnitPrice       = unitPrice,
            TotalPrice      = totalPrice,
            Currency        = "KWD",
            Status          = OrderStatus.PendingReview,
            Items           = items,
        };
    }

    private async Task<CardOrder?> GetOrderWithItemsAsync(Guid id)
    {
        var query = _unitOfWork.Repository<CardOrder>()
            .GetQueryable().AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardType)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardPackage);

        try
        {
            return await query.FirstOrDefaultAsync(o => o.Id == id);
        }
        catch (InvalidOperationException)
        {
            return query.FirstOrDefault(o => o.Id == id);
        }
    }

    private async Task<ServiceResult<List<CardOrderItem>>> BuildOrderItemsAsync(
        AssignmentScope? scope, List<Guid>? employeeIds)
    {
        if (!scope.HasValue)
            return ServiceResult<List<CardOrderItem>>.Success([]);

        if (scope == AssignmentScope.SpecificEmployees || scope == AssignmentScope.ExcelUpload)
        {
            var empCount = employeeIds?.Count ?? 0;
            if (empCount == 0)
            {
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("NoValidEmployeeRows"), 422);
            }

            var query = _unitOfWork.Repository<Employee>()
                .GetQueryable().AsNoTracking()
                .Include(e => e.UserProfile)
                .Where(e => employeeIds!.Contains(e.Id));

            List<Employee> employees;
            try
            {
                employees = await query.ToListAsync();
            }
            catch (InvalidOperationException)
            {
                employees = query.ToList();
            }

            var missingIds = employeeIds!.Except(employees.Select(e => e.Id)).ToList();
            if (missingIds.Count > 0)
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("EmployeesNotFound", string.Join(", ", missingIds)), 422);

            var employeesWithoutProfile = employees.Where(e => e.UserProfile == null).Select(e => e.FullName).ToList();
            if (employeesWithoutProfile.Count > 0)
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("EmployeesMissingProfile", string.Join("، ", employeesWithoutProfile)), 422);

            return ServiceResult<List<CardOrderItem>>.Success(
                employees.Select(e => _mapper.Map<CardOrderItem>(e)).ToList());
        }

        if (scope == AssignmentScope.AllEmployees)
        {
            var query = _unitOfWork.Repository<Employee>()
                .GetQueryable().AsNoTracking()
                .Include(e => e.UserProfile)
                .Where(e => !e.IsDeleted && e.UserProfile != null);

            List<Employee> allEmployees;
            try
            {
                allEmployees = await query.ToListAsync();
            }
            catch (InvalidOperationException)
            {
                allEmployees = query.ToList();
            }

            return ServiceResult<List<CardOrderItem>>.Success(
                allEmployees.Select(e => _mapper.Map<CardOrderItem>(e)).ToList());
        }

        return ServiceResult<List<CardOrderItem>>.Success([]);
    }

    private void EnqueueOtpNotifications(User recipient, string otp, string cardName)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (string.IsNullOrWhiteSpace(culture)) culture = "ar";

        if (!string.IsNullOrWhiteSpace(recipient.Email))
            _backgroundJobClient.Enqueue<IEmailService>(x =>
                x.SendOrderReadyOtpEmailAsync(recipient.Email, otp, cardName, culture));

        var whatsAppNumber = recipient.UserProfile?.WhatsApp;
        if (!string.IsNullOrWhiteSpace(whatsAppNumber))
            _backgroundJobClient.Enqueue<IWhatsAppService>(x =>
                x.SendWhatsAppMessageAsync(whatsAppNumber, _messageService.Get("WhatsAppNewOtp", otp)));
    }
}
