using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.Settings;

namespace NFC.Platform.Application.Services;

public class CardOrderService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ICurrentTenant currentTenant,
    ICardPricingService cardPricingService,
    IValidator<CreateCardOrderRequest> validator,
    IBackgroundJobClient backgroundJobClient,
    IOptions<OtpSettings> otpSettings) : ICardOrderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
    private readonly ICardPricingService _cardPricingService = cardPricingService ?? throw new ArgumentNullException(nameof(cardPricingService));
    private readonly IValidator<CreateCardOrderRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
    private readonly OtpSettings _otpSettings = otpSettings?.Value ?? throw new ArgumentNullException(nameof(otpSettings));

    public async Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedOrdersAsync(PaginationRequest request, string? statusFilter)
    {
        var query = _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Items)
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

    public async Task<ServiceResult<CardOrderDto>> GetOrderByIdAsync(Guid id)
    {
        var order = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

        return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(order));
    }

    public async Task<ServiceResult<CardOrderDto>> CreateOrderAsync(CreateCardOrderRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return ServiceResult<CardOrderDto>.Fail(errors, 422);
        }

        var userId = _currentTenant.UserId;
        if (!userId.HasValue)
            return ServiceResult<CardOrderDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

        // ── Pricing ──────────────────────────────────────────────────────
        var pricingResult = await _cardPricingService.CalculateOrderPricingAsync(request.CardType, request.Quantity);
        if (!pricingResult.IsSuccess)
        {
            return ServiceResult<CardOrderDto>.Fail(pricingResult.Message, pricingResult.StatusCode);
        }
        var pricing = pricingResult.Data;

        // ── Build Order ───────────────────────────────────────────────────
        var order = _mapper.Map<CardOrder>(request);
        order.UserId = userId.Value;
        
        order.UnitPrice = pricing.UnitPrice;
        order.TotalPrice = pricing.TotalPrice;
        order.Currency = pricing.Currency;
        
        order.Status = request.CardDesignType == CardDesignType.CustomArtwork
            ? OrderStatus.PendingReview
            : OrderStatus.AwaitingDesign;

        // TenantId is auto-assigned by DbContext.ApplyTenantRules() on SaveChanges
        await _unitOfWork.Repository<CardOrder>().AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // ── Excel import (CompanyAdmin only) ──────────────────────────────
        var currentUser = await _unitOfWork.Repository<User>()
            .GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (currentUser?.AccountType == AccountType.CompanyAdmin
            && !string.IsNullOrWhiteSpace(request.ExcelDataUrl))
        {
            var tenantId = _currentTenant.TenantId!.Value;
            var job = new EmployeeImportJob
            {
                TenantId          = tenantId,
                UserId            = userId.Value,
                Status            = EmployeeImportJobStatus.Pending,
                FileName          = "excel-upload",
                ExcelFileUrl      = request.ExcelDataUrl,
                CardType          = request.CardType,
                CardDesignType    = request.CardDesignType,
                Notes             = request.Notes,
                CardOrderId       = order.Id,
            };

            await _unitOfWork.Repository<EmployeeImportJob>().AddAsync(job);
            await _unitOfWork.SaveChangesAsync();

            _backgroundJobClient.Enqueue<IEmployeeImportService>(s => s.ProcessImportJobAsync(job.Id));
        }

        // Reload with items for the response
        var created = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable().AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        return ServiceResult<CardOrderDto>.Success(
            _mapper.Map<CardOrderDto>(created),
            _messageService.Get("RecordCreated"));
    }


    public async Task<ServiceResult> DeleteOrderAsync(Guid id)
    {
        var repo = _unitOfWork.Repository<CardOrder>();
        var order = await repo.GetByIdAsync(id);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        // Only allow deletion while the order is still awaiting the admin's first look
        if (order.Status != OrderStatus.PendingReview)
            return ServiceResult.Fail(_messageService.Get("OrderCannotBeCancelled"), 400);

        repo.Remove(order);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordDeleted"));
    }

    private async Task<ServiceResult<List<CardOrderItem>>> BuildOrderItemsAsync(
        AssignmentScope? scope, List<Guid>? employeeIds, int quantity)
    {
        if (!scope.HasValue)
            return ServiceResult<List<CardOrderItem>>.Success(new List<CardOrderItem>());

        if (scope == AssignmentScope.SpecificEmployees)
        {
            if (employeeIds == null || employeeIds.Count != quantity)
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("EmployeeCountMismatch", (employeeIds?.Count ?? 0).ToString(), quantity.ToString()), 422);

            var employees = await _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .AsNoTracking()
                .Include(e => e.UserProfile)
                .Where(e => employeeIds.Contains(e.Id))
                .ToListAsync();

            var missingIds = employeeIds.Except(employees.Select(e => e.Id)).ToList();
            if (missingIds.Count > 0)
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("EmployeesNotFound", string.Join(", ", missingIds)), 422);

            var employeesWithoutProfile = employees.Where(e => e.UserProfile == null).Select(e => e.Id).ToList();
            if (employeesWithoutProfile.Count > 0)
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("EmployeesMissingProfile", string.Join(", ", employeesWithoutProfile)), 422);

            return ServiceResult<List<CardOrderItem>>.Success(
                employees.Select(e => _mapper.Map<CardOrderItem>(e)).ToList());
        }

        if (scope == AssignmentScope.AllEmployees)
        {
            var allEmployees = await _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .AsNoTracking()
                .Include(e => e.UserProfile)
                .Where(e => !e.IsDeleted && e.UserProfile != null)
                .ToListAsync();

            if (quantity != allEmployees.Count)
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("EmployeeCountMismatch", allEmployees.Count.ToString(), quantity.ToString()), 422);

            return ServiceResult<List<CardOrderItem>>.Success(
                allEmployees.Select(e => _mapper.Map<CardOrderItem>(e)).ToList());
        }

        return ServiceResult<List<CardOrderItem>>.Success(new List<CardOrderItem>());
    }

    public async Task<ServiceResult<CardOrderDto>> CreateReorderAsync(Guid parentOrderId, ReorderRequest request)
    {
        var userId = _currentTenant.UserId;
        if (!userId.HasValue)
            return ServiceResult<CardOrderDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

        if (request.DeliveryMethod == DeliveryMethod.Courier && string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            return ServiceResult<CardOrderDto>.Fail(_messageService.Get("ShippingAddressRequired"), 422);
        }

        var parentOrder = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == parentOrderId);

        if (parentOrder == null)
            return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

        var itemsResult = await BuildOrderItemsAsync(request.AssignmentScope, request.EmployeeIds, request.Quantity);
        if (!itemsResult.IsSuccess)
            return ServiceResult<CardOrderDto>.Fail(itemsResult.Message ?? string.Join(", ", itemsResult.Errors), itemsResult.StatusCode);

        var pricingResult = await _cardPricingService.CalculateOrderPricingAsync(parentOrder.CardType, request.Quantity);
        if (!pricingResult.IsSuccess)
        {
            return ServiceResult<CardOrderDto>.Fail(pricingResult.Message, pricingResult.StatusCode);
        }
        var pricing = pricingResult.Data;

        var reorder = _mapper.Map<CardOrder>(parentOrder);
        reorder.UserId = userId.Value;
        reorder.ParentOrderId = parentOrderId;
        reorder.Quantity = request.Quantity;
        reorder.Status = OrderStatus.PendingReview;
        reorder.UnitPrice = pricing.UnitPrice;
        reorder.Currency = pricing.Currency;
        reorder.TotalPrice = pricing.TotalPrice;
        reorder.DeliveryMethod = request.DeliveryMethod;
        reorder.ShippingAddress = request.ShippingAddress;
        reorder.Items = itemsResult.Data ?? new List<CardOrderItem>();

        await _unitOfWork.Repository<CardOrder>().AddAsync(reorder);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<CardOrderDto>.Success(
            _mapper.Map<CardOrderDto>(reorder),
            _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult> ResendOrderOtpAsync(Guid orderId)
    {
        var tenantId = _currentTenant.TenantId;
        var orderRepo = _unitOfWork.Repository<CardOrder>();
        var order = await orderRepo.GetQueryable()
            .Include(o => o.Tenant)
                .ThenInclude(t => t.Company)
                    .ThenInclude(c => c!.AdminUser)
                        .ThenInclude(u => u!.UserProfile)
            .Include(o => o.User)
                .ThenInclude(u => u.UserProfile)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.ReadyForDelivery)
            return ServiceResult.Fail(_messageService.Get("OrderNotReadyForDelivery"), 422);

        // Enforce 60-second cooldown rate limit
        if (order.DeliveryOtpLastSentAt.HasValue &&
            (DateTime.UtcNow - order.DeliveryOtpLastSentAt.Value).TotalSeconds < _otpSettings.CooldownSeconds)
        {
            return ServiceResult.Fail(_messageService.Get("OtpCooldownActive"), 422);
        }

        // Enforce maximum resend attempts per order
        if (order.DeliveryOtpResendCount >= _otpSettings.MaxResendAttempts)
        {
            return ServiceResult.Fail(_messageService.Get("OtpResendLimitReached"), 422);
        }

        var recipient = order.Tenant?.Company?.AdminUser ?? order.User;

        var newOtp = Random.Shared.Next(100000, 999999).ToString();
        order.DeliveryOtp = newOtp;
        order.DeliveryOtpExpiresAt = DateTime.UtcNow.AddDays(7);
        order.DeliveryOtpLastSentAt = DateTime.UtcNow;
        order.DeliveryOtpResendCount++;

        await _unitOfWork.SaveChangesAsync();

        if (recipient != null)
        {
            EnqueueOtpNotifications(recipient, newOtp, order.CardName);
        }

        return ServiceResult.Success(_messageService.Get("OtpResent"));
    }

    private void EnqueueOtpNotifications(User recipient, string otp, string cardName)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (string.IsNullOrWhiteSpace(culture)) culture = "ar";

        if (!string.IsNullOrWhiteSpace(recipient.Email))
        {
            _backgroundJobClient.Enqueue<IEmailService>(x =>
                x.SendOrderReadyOtpEmailAsync(recipient.Email, otp, cardName, culture));
        }

        var whatsAppNumber = recipient.UserProfile?.WhatsApp;
        if (!string.IsNullOrWhiteSpace(whatsAppNumber))
        {
            var waMessage = _messageService.Get("WhatsAppNewOtp", otp);
            _backgroundJobClient.Enqueue<IWhatsAppService>(x =>
                x.SendWhatsAppMessageAsync(whatsAppNumber, waMessage));
        }
    }
}
