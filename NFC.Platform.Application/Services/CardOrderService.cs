namespace NFC.Platform.Application.Services;

    public class CardOrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant,
        IExcelParser excelParser,
        IValidator<CreateCardOrderRequest> validator,
        IStorageService storageService,
        IBackgroundJobClient backgroundJobClient) : ICardOrderService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        private readonly IExcelParser _excelParser = excelParser ?? throw new ArgumentNullException(nameof(excelParser));
        private readonly FluentValidation.IValidator<CreateCardOrderRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));

        public async Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedAsync(PaginationRequest request, string? statusFilter)
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

        public async Task<ServiceResult<CardOrderDto>> GetByIdAsync(Guid id)
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

        public async Task<ServiceResult<CardOrderDto>> CreateAsync(CreateCardOrderRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<CardOrderDto>.Fail(errors, 422);
            }

            if (request.DeliveryMethod == DeliveryMethod.Courier && string.IsNullOrWhiteSpace(request.ShippingAddress))
            {
                return ServiceResult<CardOrderDto>.Fail(_messageService.Get("ShippingAddressRequired"), 422);
            }

            var userId = _currentTenant.UserId;

            if (!userId.HasValue)
                return ServiceResult<CardOrderDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

            var itemsResult = await BuildOrderItemsAsync(request.AssignmentScope, request.EmployeeIds, request.Quantity);
            if (!itemsResult.IsSuccess)
            {
                return ServiceResult<CardOrderDto>.Fail(itemsResult.Message ?? string.Join(", ", itemsResult.Errors), itemsResult.StatusCode);
            }

            var order = _mapper.Map<CardOrder>(request);
            order.UserId = userId.Value;
            order.Items = itemsResult.Data ?? new List<CardOrderItem>();

            // Apply defaults for fields not supplied by the simple UI modal
            if (string.IsNullOrWhiteSpace(order.CardName))
            {
                order.CardName = _messageService.Get("DefaultCardOrderName", order.Quantity.ToString()) ?? $"Card Order - {order.Quantity}";
            }
            if (order.CardType.HasValue)
            {
                var pricing = await _unitOfWork.Repository<CardPricing>().GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.CardType == order.CardType.Value && p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
                    .OrderByDescending(p => p.EffectiveFrom)
                    .FirstOrDefaultAsync();

                if (pricing == null)
                {
                    return ServiceResult<CardOrderDto>.Fail(
                        _messageService.Get("PricingNotConfigured") ?? $"Pricing is not configured for card type '{order.CardType}'.",
                        500);
                }

                order.UnitPrice = pricing.UnitPrice;
                order.Currency = pricing.Currency;
                order.TotalPrice = pricing.UnitPrice * order.Quantity;
            }
            else
            {
                order.UnitPrice = 0;
                order.TotalPrice = 0;
            }

            // TenantId is auto-assigned by DbContext.ApplyTenantRules() on SaveChanges
            // for all ITenantEntity entries with TenantId == Guid.Empty

            await _unitOfWork.Repository<CardOrder>().AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // Reload with items for the response
            var created = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            return ServiceResult<CardOrderDto>.Success(
                _mapper.Map<CardOrderDto>(created),
                _messageService.Get("RecordCreated"));
        }


        public async Task<ServiceResult> DeleteAsync(Guid id)
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

        public async Task<ServiceResult<OrderPricingResponseDto>> GetOrderPricingAsync(string cardType, int quantity)
        {
            CardType resolvedCardType;
            string normalizedInput = cardType?.Trim().ToLower() ?? "";
            if (normalizedInput == "plastic")
            {
                resolvedCardType = CardType.Plastic;
            }
            else if (normalizedInput == "metal")
            {
                resolvedCardType = CardType.Metal;
            }
            else if (normalizedInput == "wood" || normalizedInput == "wooden")
            {
                resolvedCardType = CardType.Wooden;
            }
            else if (normalizedInput == "custom")
            {
                resolvedCardType = CardType.Custom;
            }
            else
            {
                return ServiceResult<OrderPricingResponseDto>.Fail(
                    _messageService.Get("InvalidCardType") ?? $"Invalid card type: '{cardType}'. Supported types are Plastic, Wood, and Metal.",
                    400);
            }

            var pricing = await _unitOfWork.Repository<CardPricing>().GetQueryable()
                .AsNoTracking()
                .Where(p => p.CardType == resolvedCardType && p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (pricing == null)
            {
                return ServiceResult<OrderPricingResponseDto>.Fail(
                    _messageService.Get("PricingNotConfigured") ?? $"Pricing is not configured for card type '{resolvedCardType}'.",
                    500);
            }

            var dto = new OrderPricingResponseDto
            {
                UnitPrice = pricing.UnitPrice,
                TotalPrice = pricing.UnitPrice * quantity,
                Currency = pricing.Currency
            };

            return ServiceResult<OrderPricingResponseDto>.Success(dto);
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

            var pricing = await _unitOfWork.Repository<CardPricing>().GetQueryable()
                .AsNoTracking()
                .Where(p => p.CardType == parentOrder.CardType && p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (pricing == null)
            {
                return ServiceResult<CardOrderDto>.Fail(
                    _messageService.Get("PricingNotConfigured") ?? $"Pricing is not configured for card type '{parentOrder.CardType}'.",
                    500);
            }

            var reorder = _mapper.Map<CardOrder>(parentOrder);
            reorder.UserId = userId.Value;
            reorder.ParentOrderId = parentOrderId;
            reorder.Quantity = request.Quantity;
            reorder.Status = OrderStatus.PendingReview;
            reorder.UnitPrice = pricing.UnitPrice;
            reorder.Currency = pricing.Currency;
            reorder.TotalPrice = pricing.UnitPrice * request.Quantity;
            reorder.DeliveryMethod = request.DeliveryMethod;
            reorder.ShippingAddress = request.ShippingAddress;
            reorder.Items = itemsResult.Data ?? new List<CardOrderItem>();

            await _unitOfWork.Repository<CardOrder>().AddAsync(reorder);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<CardOrderDto>.Success(
                _mapper.Map<CardOrderDto>(reorder),
                _messageService.Get("RecordCreated"));
        }




        public async Task<ServiceResult<EmployeeImportJob>> QueueEmployeeImportJobAsync(
            Microsoft.AspNetCore.Http.IFormFile file,
            CardType cardType,
            CardDesignType cardDesignType,
            string? notes)
        {
            var tenantId = _currentTenant.TenantId;
            var userId = _currentTenant.UserId;

            if (!tenantId.HasValue || !userId.HasValue)
                return ServiceResult<EmployeeImportJob>.Unauthorized(_messageService.Get("Unauthorized"));



            if (file == null || file.Length == 0)
                return ServiceResult<EmployeeImportJob>.Fail(_messageService.Get("NoFileUploaded"), 400);

            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return ServiceResult<EmployeeImportJob>.Fail(_messageService.Get("ExcelFilesOnly"), 400);

            // Upload Excel file to Cloudinary raw storage
            var uploadResult = await _storageService.UploadRawFileAsync(file, "employee-imports");
            if (string.IsNullOrWhiteSpace(uploadResult.SecureUrl))
            {
                return ServiceResult<EmployeeImportJob>.Fail(_messageService.Get("FileUploadFailed"), 500);
            }

            var job = new EmployeeImportJob
            {
                TenantId = tenantId.Value,
                UserId = userId.Value,
                Status = EmployeeImportJobStatus.Pending,
                FileName = file.FileName,
                ExcelFileUrl = uploadResult.SecureUrl,
                ExcelFilePublicId = uploadResult.PublicId,
                CardType = cardType,
                CardDesignType = cardDesignType,
                Notes = notes
            };

            await _unitOfWork.Repository<EmployeeImportJob>().AddAsync(job);
            await _unitOfWork.SaveChangesAsync();

            // Enqueue Hangfire background job
            _backgroundJobClient.Enqueue<ICardOrderService>(s => s.ProcessEmployeeImportJobAsync(job.Id));

            return ServiceResult<EmployeeImportJob>.Success(job, _messageService.Get("RecordCreated"));
        }

        public async Task ProcessEmployeeImportJobAsync(Guid jobId)
        {
            // Use IgnoreQueryFilters because the tenant context is not set yet.
            var job = await _unitOfWork.Repository<EmployeeImportJob>()
                .GetQueryable()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null) return;

            // Idempotency check: If job is already processing, completed, or failed, exit early.
            if (job.Status != EmployeeImportJobStatus.Pending) return;

            // Update status to Processing
            job.Status = EmployeeImportJobStatus.Processing;
            job.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            // Set current tenant/user context on the thread
            _currentTenant.SetCurrentTenant(job.TenantId, job.UserId);

            List<string> errors = new();

            try
            {
                // 1. Download file
                using var httpClient = new System.Net.Http.HttpClient();
                byte[] fileBytes;
                try
                {
                    fileBytes = await httpClient.GetByteArrayAsync(job.ExcelFileUrl);
                }
                catch (Exception ex)
                {
                    var pattern = _messageService.Get("FailedToDownloadExcel");
                    errors.Add(string.Format(pattern, ex.Message));
                    await FailJobAsync(job, errors);
                    return;
                }

                using var stream = new System.IO.MemoryStream(fileBytes);

                // 2. Parse Excel
                List<ExcelEmployeeImportDto> employeeRows;
                try
                {
                    employeeRows = _excelParser.ParseEmployeesFromExcel(stream);
                }
                catch (Exception ex)
                {
                    var pattern = _messageService.Get("FailedToParseExcel");
                    errors.Add(string.Format(pattern, ex.Message));
                    await FailJobAsync(job, errors);
                    return;
                }

                if (employeeRows == null || employeeRows.Count == 0)
                {
                    errors.Add(_messageService.Get("NoValidEmployeeRows"));
                    await FailJobAsync(job, errors);
                    return;
                }

                job.TotalRows = employeeRows.Count;
                await _unitOfWork.SaveChangesAsync();

                // 3. Row-level validation
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var uniqueEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < employeeRows.Count; i++)
                {
                    var row = employeeRows[i];
                    var rowNum = i + 2; // Data starts on row 2 (row 1 is header)
                    if (string.IsNullOrWhiteSpace(row.Name))
                    {
                        var pattern = _messageService.Get("ImportRowNameRequired");
                        errors.Add(string.Format(pattern, rowNum));
                    }
                    if (string.IsNullOrWhiteSpace(row.Email))
                    {
                        var pattern = _messageService.Get("ImportRowEmailRequired");
                        errors.Add(string.Format(pattern, rowNum));
                    }
                    else
                    {
                        if (!emailRegex.IsMatch(row.Email))
                        {
                            var pattern = _messageService.Get("ImportRowEmailInvalid");
                            errors.Add(string.Format(pattern, rowNum, row.Email));
                        }
                        else if (!uniqueEmails.Add(row.Email))
                        {
                            var pattern = _messageService.Get("ImportRowEmailDuplicate");
                            errors.Add(string.Format(pattern, rowNum, row.Email));
                        }
                    }
                }

                if (errors.Count > 0)
                {
                    await FailJobAsync(job, errors);
                    return;
                }

                // 4. Fetch Company and Subscription limits
                var company = await _unitOfWork.Repository<Company>()
                    .GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.TenantId == job.TenantId);

                if (company == null)
                {
                    errors.Add(_messageService.Get("CompanyNotFound"));
                    await FailJobAsync(job, errors);
                    return;
                }

                var activeSub = await _unitOfWork.Repository<UserSubscription>()
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(s => s.SubscriptionPlan)
                    .FirstOrDefaultAsync(s => s.TenantId == job.TenantId && s.IsActive && s.EndDate >= DateTime.UtcNow);

                if (activeSub == null)
                {
                    errors.Add(_messageService.Get("SubscriptionExpiredOrMissing"));
                    await FailJobAsync(job, errors);
                    return;
                }

                var currentEmployeesCount = await _unitOfWork.Repository<Employee>()
                    .CountAsync(e => e.TenantId == job.TenantId && !e.IsDeleted);

                // 5. Transaction processing
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var targetEmails = employeeRows.Select(r => r.Email).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    var existingUserProfilesList = await _unitOfWork.Repository<User>()
                        .GetQueryable()
                        .AsNoTracking()
                        .Include(u => u.UserProfile)
                        .Where(u => u.TenantId == job.TenantId && targetEmails.Contains(u.Email))
                        .ToListAsync();

                    var userProfilesByEmail = existingUserProfilesList
                        .Where(u => u.UserProfile != null)
                        .ToDictionary(u => u.Email, u => u.UserProfile!, StringComparer.OrdinalIgnoreCase);

                    var employeesByEmail = await _unitOfWork.Repository<Employee>()
                        .GetQueryable()
                        .Include(e => e.UserProfile)
                        .Where(e => e.TenantId == job.TenantId && !e.IsDeleted && targetEmails.Contains(e.Email))
                        .ToDictionaryAsync(e => e.Email, e => e, StringComparer.OrdinalIgnoreCase);

                    var itemsToOrder = new List<CardOrderItem>();
                    var newEmployeesCount = 0;
                    var resolvedProfiles = new List<UserProfile>();
                    var newEmployeesList = new List<Employee>();
                    var newProfilesList = new List<UserProfile>();

                    // Pre-load all existing subdomains once â€” avoids N+1 queries in the loop.
                    // IgnoreQueryFilters: background job has no tenant context; uniqueness is global.
                    var existingSlugs = new HashSet<string>(
                        await _unitOfWork.Repository<UserProfile>()
                            .GetQueryable()
                            .IgnoreQueryFilters()
                            .Where(p => p.Subdomain != null)
                            .Select(p => p.Subdomain!)
                            .ToListAsync(),
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var row in employeeRows)
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
                            // Validate subscription limit
                            if (currentEmployeesCount + newEmployeesCount >= activeSub.SubscriptionPlan.MaxEmployees)
                            {
                                throw new BusinessException("MaxEmployeesLimitReached");
                            }

                            // Create employee record
                            var newEmployee = _mapper.Map<Employee>(row);
                            newEmployee.CompanyId = company.Id;
                            newEmployee.TenantId = job.TenantId;

                            userProfile = _mapper.Map<UserProfile>(row);
                            userProfile.CompanyName = company.Name;
                            userProfile.TenantId = job.TenantId;
                            // In-memory slug generation â€” no extra DB queries
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
                            resolvedProfiles.Add(userProfile);
                        }
                    }

                    // Bulk insert new records in a single save roundtrip
                    if (newEmployeesList.Count > 0)
                    {
                        await _unitOfWork.Repository<Employee>().AddRangeAsync(newEmployeesList);
                        await _unitOfWork.Repository<UserProfile>().AddRangeAsync(newProfilesList);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    // Map resolved profiles to CardOrderItems after IDs have been generated
                    for (int i = 0; i < employeeRows.Count; i++)
                    {
                        var row = employeeRows[i];
                        UserProfile? userProfile = null;
                        if (employeesByEmail.TryGetValue(row.Email, out var emp))
                        {
                            userProfile = emp.UserProfile;
                        }
                        else if (userProfilesByEmail.TryGetValue(row.Email, out var existingProfile))
                        {
                            userProfile = existingProfile;
                        }

                        if (userProfile != null)
                        {
                            var orderItem = _mapper.Map<CardOrderItem>(row);
                            orderItem.UserProfileId = userProfile.Id;
                            orderItem.TenantId = job.TenantId;
                            itemsToOrder.Add(orderItem);
                        }
                    }

                    var pricing = await _unitOfWork.Repository<CardPricing>().GetQueryable()
                        .AsNoTracking()
                        .Where(p => p.CardType == job.CardType && p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
                        .OrderByDescending(p => p.EffectiveFrom)
                        .FirstOrDefaultAsync();

                    if (pricing == null)
                    {
                        throw new Exception($"Pricing is not configured for card type '{job.CardType}'.");
                    }

                    var cardOrder = new CardOrder
                    {
                        CardName = _messageService.Get("DefaultBulkOrderName", itemsToOrder.Count.ToString()) ?? $"Bulk Order - {itemsToOrder.Count}",
                        CardType = job.CardType,
                        CardDesignType = job.CardDesignType,
                        Quantity = itemsToOrder.Count,
                        Notes = job.Notes,
                        UserId = job.UserId,
                        TenantId = job.TenantId,
                        Status = OrderStatus.PendingReview,
                        Items = itemsToOrder,
                        UnitPrice = pricing.UnitPrice,
                        Currency = pricing.Currency,
                        TotalPrice = pricing.UnitPrice * itemsToOrder.Count
                    };

                    await _unitOfWork.Repository<CardOrder>().AddAsync(cardOrder);
                    await _unitOfWork.SaveChangesAsync();

                    // Update job tracking status
                    job.Imported = resolvedProfiles.Count;
                    job.Skipped = employeeRows.Count - resolvedProfiles.Count;
                    job.CardOrderId = cardOrder.Id;
                    job.Status = EmployeeImportJobStatus.Completed;
                    job.ErrorsJson = null;
                    job.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();

                    await _unitOfWork.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    if (ex.Message == "MaxEmployeesLimitReached")
                    {
                        errors.Add(_messageService.Get("MaxEmployeesLimitReached"));
                    }
                    else
                    {
                        var pattern = _messageService.Get("ImportExecutionFailed");
                        errors.Add(string.Format(pattern, ex.Message));
                    }
                    await FailJobAsync(job, errors);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"System error during background processing: {ex.Message}");
                await FailJobAsync(job, errors);
            }
        }

        private async Task FailJobAsync(EmployeeImportJob job, List<string> errors)
        {
            try
            {
                job.Status = EmployeeImportJobStatus.Failed;
                job.ErrorsJson = System.Text.Json.JsonSerializer.Serialize(errors);
                job.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write failure state for job {job.Id}: {ex.Message}");
            }
        }

        public async Task<ServiceResult<EmployeesImportStatusDto>> GetEmployeesImportStatusAsync(Guid id)
        {
            // 1. Try to find the import job first
            var job = await _unitOfWork.Repository<EmployeeImportJob>()
                .GetQueryable()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job != null)
            {
                var dto = _mapper.Map<EmployeesImportStatusDto>(job);
                return ServiceResult<EmployeesImportStatusDto>.Success(dto);
            }

            // 2. Fallback to check CardOrder
            var order = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return ServiceResult<EmployeesImportStatusDto>.NotFound(
                    _messageService.Get("RecordNotFound"));
            }

            var orderDto = _mapper.Map<EmployeesImportStatusDto>(order);
            orderDto.Status = "Completed";

            return ServiceResult<EmployeesImportStatusDto>.Success(orderDto);
        }

        public async Task<ServiceResult<IReadOnlyList<CardPricingDto>>> GetActivePricingCatalogAsync()
        {
            var pricings = await _unitOfWork.Repository<CardPricing>().GetQueryable()
                .AsNoTracking()
                .Where(p => p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
                .ToListAsync();

            var dtos = _mapper.Map<IReadOnlyList<CardPricingDto>>(pricings);

            return ServiceResult<IReadOnlyList<CardPricingDto>>.Success(dtos);
        }

        public async Task<ServiceResult> ResendDeliveryOtpAsync(Guid orderId)
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
                (DateTime.UtcNow - order.DeliveryOtpLastSentAt.Value).TotalSeconds < 60)
            {
                return ServiceResult.Fail(_messageService.Get("OtpCooldownActive"), 422);
            }

            // Enforce maximum 5 resend attempts per order
            if (order.DeliveryOtpResendCount >= 5)
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

