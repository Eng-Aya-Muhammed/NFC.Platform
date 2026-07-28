

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
    IOptions<OtpSettings> otpSettings) : ICardOrderService
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

    // Queries

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
        var order = await GetOrderWithItemsAsync(id);

        if (order == null)
            return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

        return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(order));
    }

    // Commands

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
            // 1. Validate CardType (exists & IsActive)
            if (request.CardTypeId != Guid.Empty)
            {
                var cardType = await _unitOfWork.Repository<CardType>().GetByIdAsync(request.CardTypeId);
                if (cardType != null && !cardType.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardOrderDto>.Fail(_messageService.Get("InvalidOrInactiveCardType"), 400);
                }
            }

            // 2. Validate CardPackage (exists & IsActive)
            var cardPackage = await _unitOfWork.Repository<CardPackage>().GetByIdAsync(request.CardPackageId);
            if (cardPackage == null || !cardPackage.IsActive)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<CardOrderDto>.Fail(_messageService.Get("InvalidOrInactiveCardPackage"), 400);
            }

            // 3. Build CardOrderItems (from Assignment Scope / Excel Upload)
            var itemsToOrder = new List<CardOrderItem>();

            if (request.AssignmentScope == AssignmentScope.ExcelUpload && !string.IsNullOrWhiteSpace(request.ExcelDataUrl))
            {
                var company = await _unitOfWork.Repository<Company>().GetQueryable().AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId.Value);
                if (company == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardOrderDto>.Fail(_messageService.Get("CompanyNotFound"), 400);
                }

                var excelResult = await _employeeService.UpsertEmployeesFromExcelAsync(request.ExcelDataUrl, company.Id, tenantId.Value);
                if (!excelResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardOrderDto>.Fail(excelResult.Message ?? string.Join(", ", excelResult.Errors), excelResult.StatusCode);
                }
                
                request.EmployeeIds = excelResult.Data;
            }

            var itemsResult = await BuildOrderItemsAsync(request.AssignmentScope, request.EmployeeIds);
            if (!itemsResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<CardOrderDto>.Fail(itemsResult.Message ?? string.Join(", ", itemsResult.Errors), itemsResult.StatusCode);
            }

            itemsToOrder = itemsResult.Data ?? [];

            // 4. Calculate Required Physical Cards using Sum(NumberOfCardsRequired) for items where RequiresCard == true
            var totalRequiredCards = itemsToOrder.Where(i => i.RequiresCard).Sum(i => i.NumberOfCardsRequired);

            // 5. Validate Package Capacity
            if (totalRequiredCards > cardPackage.NumberOfCards)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<CardOrderDto>.Fail(_messageService.Get("CardPackageCapacityExceeded", totalRequiredCards.ToString(), cardPackage.NumberOfCards.ToString()), 400);
            }

            // 6. Apply Package Pricing & Map Order via AutoMapper Profile
            var order = _mapper.Map<CardOrder>(request) ?? new CardOrder();
            order.UserId = userId.Value;
            order.TenantId = tenantId.Value;
            order.Quantity = cardPackage.NumberOfCards;
            order.TotalPrice = cardPackage.Price;
            order.UnitPrice = cardPackage.NumberOfCards > 0 ? cardPackage.Price / cardPackage.NumberOfCards : cardPackage.Price;
            order.Currency = "KWD";
            order.Status = OrderStatus.PendingReview;

            if (itemsToOrder.Count > 0)
            {
                order.Items = itemsToOrder;
            }

            // 7. Persist Order
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

        if (request.DeliveryMethod == DeliveryMethod.Courier && string.IsNullOrWhiteSpace(request.ShippingAddress))
            return ServiceResult<CardOrderDto>.Fail(_messageService.Get("ShippingAddressRequired"), 422);

        var parentOrder = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == parentOrderId);

        if (parentOrder == null)
            return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

        var packageIdToUse = request.CardPackageId ?? parentOrder.CardPackageId;
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

        var reorder = BuildReorder(parentOrder, request, userId.Value, cardPackage, itemsToOrder);
        await _unitOfWork.Repository<CardOrder>().AddAsync(reorder);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(reorder), _messageService.Get("RecordCreated"));
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
            if (request.CardTypeId.HasValue && request.CardTypeId.Value != order.CardTypeId)
            {
                var cardType = await _unitOfWork.Repository<CardType>().GetByIdAsync(request.CardTypeId.Value);
                if (cardType == null || !cardType.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardOrderDto>.Fail(_messageService.Get("InvalidOrInactiveCardType"), 400);
                }
                order.CardTypeId = request.CardTypeId.Value;
            }

            CardPackage? pkg = null;
            if (request.CardPackageId.HasValue)
            {
                pkg = await _unitOfWork.Repository<CardPackage>().GetByIdAsync(request.CardPackageId.Value);
                if (pkg == null || !pkg.IsActive)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardOrderDto>.Fail(_messageService.Get("InvalidOrInactiveCardPackage"), 400);
                }
                order.CardPackageId = request.CardPackageId.Value;
                order.Quantity = pkg.NumberOfCards;
                order.TotalPrice = pkg.Price;
                order.UnitPrice = pkg.NumberOfCards > 0 ? pkg.Price / pkg.NumberOfCards : pkg.Price;
            }
            else
            {
                pkg = await _unitOfWork.Repository<CardPackage>().GetByIdAsync(order.CardPackageId);
            }

            if (request.AssignmentScope == AssignmentScope.ExcelUpload && !string.IsNullOrWhiteSpace(request.ExcelDataUrl))
            {
                var company = await _unitOfWork.Repository<Company>().GetQueryable().AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId.Value);
                if (company == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardOrderDto>.Fail(_messageService.Get("CompanyNotFound"), 400);
                }

                var excelResult = await _employeeService.UpsertEmployeesFromExcelAsync(request.ExcelDataUrl, company.Id, tenantId.Value);
                if (!excelResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardOrderDto>.Fail(excelResult.Message ?? string.Join(", ", excelResult.Errors), excelResult.StatusCode);
                }
                request.EmployeeIds = excelResult.Data;
            }

            var currentScope = request.AssignmentScope ?? (order.Items.Count > 0 ? AssignmentScope.SpecificEmployees : AssignmentScope.Individual);

            if (request.AssignmentScope.HasValue || (request.EmployeeIds != null && request.EmployeeIds.Count > 0))
            {
                var itemsResult = await BuildOrderItemsAsync(currentScope, request.EmployeeIds);
                if (!itemsResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<CardOrderDto>.Fail(itemsResult.Message ?? string.Join(", ", itemsResult.Errors), itemsResult.StatusCode);
                }

                var newItems = itemsResult.Data ?? new List<CardOrderItem>();

                if (pkg != null)
                {
                    var totalRequiredCards = newItems.Where(i => i.RequiresCard).Sum(i => i.NumberOfCardsRequired);
                    if (totalRequiredCards > pkg.NumberOfCards)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<CardOrderDto>.Fail(_messageService.Get("CardPackageCapacityExceeded", totalRequiredCards.ToString(), pkg.NumberOfCards.ToString()), 400);
                    }
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
                    await itemRepo.AddAsync(newItem);
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
        await _unitOfWork.SaveChangesAsync();

        var recipient = order.Tenant?.Company?.AdminUser ?? order.User;
        if (recipient != null)
            EnqueueOtpNotifications(recipient, newOtp, order.CardName);

        return ServiceResult.Success(_messageService.Get("OtpResent"));
    }

    // Private helpers

    private static CardOrder BuildReorder(CardOrder parent, ReorderRequest request, Guid userId,
        CardPackage package, List<CardOrderItem> items)
    {
        var quantity = package.NumberOfCards;
        var unitPrice = package.NumberOfCards > 0 ? package.Price / package.NumberOfCards : package.Price;
        var totalPrice = package.Price;

        return new CardOrder
        {
            UserId          = userId,
            ParentOrderId   = parent.Id,
            CardDesignType  = parent.CardDesignType,
            CardTypeId      = parent.CardTypeId,
            CardPackageId   = package.Id,
            CardName        = parent.CardName,
            FrontDesignUrl  = parent.FrontDesignUrl,
            BackDesignUrl   = parent.BackDesignUrl,
            Quantity        = quantity,
            Notes           = parent.Notes,
            DeliveryMethod  = request.DeliveryMethod,
            ShippingAddress = request.ShippingAddress,
            UnitPrice       = unitPrice,
            TotalPrice      = totalPrice,
            Currency        = "KWD",
            Status          = OrderStatus.PendingReview,
            Items           = items,
        };
    }

    private async Task<CardOrder?> GetOrderWithItemsAsync(Guid id)
        => await _unitOfWork.Repository<CardOrder>()
            .GetQueryable().AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

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

            var employees = await _unitOfWork.Repository<Employee>()
                .GetQueryable().AsNoTracking()
                .Include(e => e.UserProfile)
                .Where(e => employeeIds.Contains(e.Id))
                .ToListAsync();

            var missingIds = employeeIds.Except(employees.Select(e => e.Id)).ToList();
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
            var allEmployees = await _unitOfWork.Repository<Employee>()
                .GetQueryable().AsNoTracking()
                .Include(e => e.UserProfile)
                .Where(e => !e.IsDeleted && e.UserProfile != null)
                .ToListAsync();

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
