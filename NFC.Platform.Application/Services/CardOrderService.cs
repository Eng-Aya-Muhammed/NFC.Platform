using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.Settings;
using NFC.Platform.Application.Extensions;

namespace NFC.Platform.Application.Services;

public class CardOrderService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ICurrentTenant currentTenant,
    ICardPricingService cardPricingService,
    IValidator<CreateCardOrderRequest> validator,
    IBackgroundJobClient backgroundJobClient,
    IHttpClientFactory httpClientFactory,
    IExcelParser excelParser,
    IOptions<OtpSettings> otpSettings) : ICardOrderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
    private readonly ICardPricingService _cardPricingService = cardPricingService ?? throw new ArgumentNullException(nameof(cardPricingService));
    private readonly IValidator<CreateCardOrderRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly IExcelParser _excelParser = excelParser ?? throw new ArgumentNullException(nameof(excelParser));
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

        var itemsToOrder = new List<CardOrderItem>();

        if (!string.IsNullOrWhiteSpace(request.ExcelDataUrl))
        {
            var excelResult = await ProcessExcelOrderItemsAsync(request.ExcelDataUrl, tenantId.Value);
            if (!excelResult.IsSuccess)
                return ServiceResult<CardOrderDto>.Fail(excelResult.Errors, excelResult.StatusCode);

            itemsToOrder = excelResult.Data ?? [];
            request.Quantity = itemsToOrder.Count;
        }

        var pricingResult = await _cardPricingService.CalculateOrderPricingAsync(request.CardType, request.Quantity);
        if (!pricingResult.IsSuccess)
            return ServiceResult<CardOrderDto>.Fail(pricingResult.Message, pricingResult.StatusCode);

        var order = BuildNewOrder(request, userId.Value, pricingResult.Data!);
        if (itemsToOrder.Count > 0)
        {
            order.Items = itemsToOrder;
        }

        await _unitOfWork.Repository<CardOrder>().AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        var created = await GetOrderWithItemsAsync(order.Id);
        return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(created), _messageService.Get("RecordCreated"));
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

        var itemsResult = await BuildOrderItemsAsync(request.AssignmentScope, request.EmployeeIds, request.Quantity);
        if (!itemsResult.IsSuccess)
            return ServiceResult<CardOrderDto>.Fail(itemsResult.Message ?? string.Join(", ", itemsResult.Errors), itemsResult.StatusCode);

        var pricingResult = await _cardPricingService.CalculateOrderPricingAsync(parentOrder.CardType, request.Quantity);
        if (!pricingResult.IsSuccess)
            return ServiceResult<CardOrderDto>.Fail(pricingResult.Message, pricingResult.StatusCode);

        var reorder = BuildReorder(parentOrder, request, userId.Value, pricingResult.Data!, itemsResult.Data!);
        await _unitOfWork.Repository<CardOrder>().AddAsync(reorder);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(reorder), _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult> DeleteOrderAsync(Guid id)
    {
        var repo = _unitOfWork.Repository<CardOrder>();
        var order = await repo.GetByIdAsync(id);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.PendingReview)
            return ServiceResult.Fail(_messageService.Get("OrderCannotBeCancelled"), 400);

        repo.Remove(order);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordDeleted"));
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

    private async Task<ServiceResult<List<CardOrderItem>>> ProcessExcelOrderItemsAsync(string excelDataUrl, Guid tenantId)
    {
        var excelValidation = await ValidateExcelAsync(excelDataUrl);
        if (!excelValidation.IsSuccess)
            return ServiceResult<List<CardOrderItem>>.Fail(excelValidation.Errors, 422);

        var excelRows = excelValidation.Data!;
        if (excelRows.Count == 0)
            return ServiceResult<List<CardOrderItem>>.Success([]);

        var company = await _unitOfWork.Repository<Company>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (company == null)
            return ServiceResult<List<CardOrderItem>>.Fail(_messageService.Get("CompanyNotFound"), 422);

        var activeSub = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .AsNoTracking()
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive && s.EndDate >= DateTime.UtcNow);

        if (activeSub == null)
            return ServiceResult<List<CardOrderItem>>.Fail(_messageService.Get("SubscriptionExpiredOrMissing"), 422);

        var employeeProcessingResult = await ProcessEmployeesAndProfilesAsync(excelRows, company, activeSub, tenantId);
        if (!employeeProcessingResult.IsSuccess)
            return ServiceResult<List<CardOrderItem>>.Fail(employeeProcessingResult.Message, employeeProcessingResult.StatusCode);

        var userProfilesByEmail = employeeProcessingResult.Data!;
        
        var itemsToOrder = new List<CardOrderItem>();
        foreach (var row in excelRows)
        {
            if (userProfilesByEmail.TryGetValue(row.Email, out var userProfile) && userProfile != null)
            {
                var orderItem = _mapper.Map<CardOrderItem>(row);
                orderItem.UserProfileId = userProfile.Id;
                orderItem.TenantId = tenantId;
                itemsToOrder.Add(orderItem);
            }
        }

        return ServiceResult<List<CardOrderItem>>.Success(itemsToOrder);
    }

    private async Task<ServiceResult<Dictionary<string, UserProfile>>> ProcessEmployeesAndProfilesAsync(
        List<ExcelEmployeeImportDto> excelRows, Company company, UserSubscription activeSub, Guid tenantId)
    {
        var currentEmployeesCount = await _unitOfWork.Repository<Employee>()
            .CountAsync(e => e.TenantId == tenantId && !e.IsDeleted);

        var targetEmails = excelRows.Select(r => r.Email).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var existingUserProfilesList = await _unitOfWork.Repository<User>()
            .GetQueryable()
            .AsNoTracking()
            .Include(u => u.UserProfile)
            .Where(u => u.TenantId == tenantId && targetEmails.Contains(u.Email))
            .ToListAsync();

        var userProfilesByEmail = existingUserProfilesList
            .Where(u => u.UserProfile != null)
            .ToDictionary(u => u.Email, u => u.UserProfile!, StringComparer.OrdinalIgnoreCase);

        var employeesByEmail = await _unitOfWork.Repository<Employee>()
            .GetQueryable()
            .Include(e => e.UserProfile)
            .Where(e => e.TenantId == tenantId && !e.IsDeleted && targetEmails.Contains(e.Email))
            .ToDictionaryAsync(e => e.Email, e => e, StringComparer.OrdinalIgnoreCase);

        var existingSlugs = new HashSet<string>(
            await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .IgnoreQueryFilters()
                .Where(p => p.Subdomain != null)
                .Select(p => p.Subdomain!)
                .ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        var newEmployeesList = new List<Employee>();
        var newProfilesList = new List<UserProfile>();
        var newEmployeesCount = 0;

        var profileMap = new Dictionary<string, UserProfile>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in excelRows)
        {
            UserProfile? userProfile = null;

            if (employeesByEmail.TryGetValue(row.Email, out var existingEmployee))
            {
                userProfile = existingEmployee.UserProfile;
            }
            else if (userProfilesByEmail.TryGetValue(row.Email, out var existingProfile))
            {
                userProfile = existingProfile;
            }
            else
            {
                if (currentEmployeesCount + newEmployeesCount >= activeSub.SubscriptionPlan.MaxEmployees)
                {
                    return ServiceResult<Dictionary<string, UserProfile>>.Fail(_messageService.Get("MaxEmployeesLimitReached"), 422);
                }

                var newEmployee = _mapper.Map<Employee>(row);
                newEmployee.CompanyId = company.Id;
                newEmployee.TenantId = tenantId;

                userProfile = _mapper.Map<UserProfile>(row);
                userProfile.CompanyName = company.Name;
                userProfile.TenantId = tenantId;
                userProfile.Subdomain = SubdomainHelper.GenerateUnique(row.Name, existingSlugs);

                newEmployee.UserProfile = userProfile;
                userProfile.Employee = newEmployee;

                newEmployeesList.Add(newEmployee);
                newProfilesList.Add(userProfile);
                newEmployeesCount++;

                employeesByEmail[row.Email] = newEmployee;
            }

            if (userProfile != null)
            {
                profileMap[row.Email] = userProfile;
            }
        }

        if (newEmployeesList.Count > 0)
        {
            await _unitOfWork.Repository<Employee>().AddRangeAsync(newEmployeesList);
            await _unitOfWork.Repository<UserProfile>().AddRangeAsync(newProfilesList);
            await _unitOfWork.SaveChangesAsync();
        }

        return ServiceResult<Dictionary<string, UserProfile>>.Success(profileMap);
    }

    private async Task<ServiceResult<List<ExcelEmployeeImportDto>>> ValidateExcelAsync(string excelUrl)
    {
        List<ExcelEmployeeImportDto> rows;
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var fileBytes = await httpClient.GetByteArrayAsync(excelUrl);
            using var stream = new MemoryStream(fileBytes);
            rows = _excelParser.ParseEmployeesFromExcel(stream);
        }
        catch (HttpRequestException)
        {
            return ServiceResult<List<ExcelEmployeeImportDto>>.Fail(_messageService.Get("FailedToDownloadExcel", excelUrl), 422);
        }
        catch (Exception)
        {
            return ServiceResult<List<ExcelEmployeeImportDto>>.Fail(_messageService.Get("FailedToParseExcel", excelUrl), 422);
        }

        if (rows == null || rows.Count == 0)
            return ServiceResult<List<ExcelEmployeeImportDto>>.Fail(_messageService.Get("NoValidEmployeeRows"), 422);

        var errors = ValidateExcelRows(rows);
        return errors.Count > 0
            ? ServiceResult<List<ExcelEmployeeImportDto>>.Fail(errors, 422)
            : ServiceResult<List<ExcelEmployeeImportDto>>.Success(rows);
    }

    private List<string> ValidateExcelRows(List<ExcelEmployeeImportDto> rows)
    {
        var errors = new List<string>();
        var emailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var uniqueEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNum = i + 2;

            if (string.IsNullOrWhiteSpace(row.Name))
                errors.Add(_messageService.Get("ImportRowNameRequired", rowNum));

            if (string.IsNullOrWhiteSpace(row.Email))
                errors.Add(_messageService.Get("ImportRowEmailRequired", rowNum));
            else if (!emailRegex.IsMatch(row.Email))
                errors.Add(_messageService.Get("ImportRowEmailInvalid", rowNum, row.Email));
            else if (!uniqueEmails.Add(row.Email))
                errors.Add(_messageService.Get("ImportRowEmailDuplicate", rowNum, row.Email));
        }

        return errors;
    }

    private static CardOrder BuildNewOrder(CreateCardOrderRequest request, Guid userId, OrderPricingResponseDto pricing)
    {
        return new CardOrder
        {
            UserId         = userId,
            CardDesignType = request.CardDesignType,
            CardType       = request.CardType,
            CardName       = request.CardName ?? string.Empty,
            ExcelDataUrl   = request.ExcelDataUrl,
            FrontDesignUrl = request.FrontDesignUrl,
            BackDesignUrl  = request.BackDesignUrl,
            Quantity       = request.Quantity,
            Notes          = request.Notes,
            UnitPrice      = pricing.UnitPrice,
            TotalPrice     = pricing.TotalPrice,
            Currency       = pricing.Currency,
            Status         = request.CardDesignType == CardDesignType.CustomArtwork
                                 ? OrderStatus.PendingReview
                                 : OrderStatus.AwaitingDesign,
        };
    }

    private static CardOrder BuildReorder(CardOrder parent, ReorderRequest request, Guid userId,
        OrderPricingResponseDto pricing, List<CardOrderItem> items)
    {
        return new CardOrder
        {
            UserId          = userId,
            ParentOrderId   = parent.Id,
            CardDesignType  = parent.CardDesignType,
            CardType        = parent.CardType,
            CardName        = parent.CardName,
            FrontDesignUrl  = parent.FrontDesignUrl,
            BackDesignUrl   = parent.BackDesignUrl,
            Quantity        = request.Quantity,
            Notes           = parent.Notes,
            DeliveryMethod  = request.DeliveryMethod,
            ShippingAddress = request.ShippingAddress,
            UnitPrice       = pricing.UnitPrice,
            TotalPrice      = pricing.TotalPrice,
            Currency        = pricing.Currency,
            Status          = OrderStatus.PendingReview,
            Items           = items,
        };
    }

    private async Task<CardOrder?> GetOrderWithItemsAsync(Guid id)
        => await _unitOfWork.Repository<CardOrder>()
            .GetQueryable().AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

    private async Task<User?> GetUserByIdAsync(Guid userId)
        => await _unitOfWork.Repository<User>()
            .GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

    private async Task<ServiceResult<List<CardOrderItem>>> BuildOrderItemsAsync(
        AssignmentScope? scope, List<Guid>? employeeIds, int quantity)
    {
        if (!scope.HasValue)
            return ServiceResult<List<CardOrderItem>>.Success([]);

        if (scope == AssignmentScope.SpecificEmployees)
        {
            if (employeeIds == null || employeeIds.Count != quantity)
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("EmployeeCountMismatch", (employeeIds?.Count ?? 0).ToString(), quantity.ToString()), 422);

            var employees = await _unitOfWork.Repository<Employee>()
                .GetQueryable().AsNoTracking()
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
                .GetQueryable().AsNoTracking()
                .Include(e => e.UserProfile)
                .Where(e => !e.IsDeleted && e.UserProfile != null)
                .ToListAsync();

            if (quantity != allEmployees.Count)
                return ServiceResult<List<CardOrderItem>>.Fail(
                    _messageService.Get("EmployeeCountMismatch", allEmployees.Count.ToString(), quantity.ToString()), 422);

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
