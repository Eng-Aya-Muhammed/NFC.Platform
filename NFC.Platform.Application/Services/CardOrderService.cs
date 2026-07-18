namespace NFC.Platform.Application.Services;

    public class CardOrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant,
        IExcelParser excelParser,
        FluentValidation.IValidator<CreateCardOrderRequest> validator,
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

            var order = _mapper.Map<CardOrder>(request);
            order.UserId = userId.Value;

            // Apply defaults for fields not supplied by the simple UI modal
            if (string.IsNullOrWhiteSpace(order.CardName))
            {
                order.CardName = _messageService.Get("DefaultCardOrderName", order.Quantity.ToString()) ?? $"Card Order - {order.Quantity}";
            }
            if (order.CardType == 0)
            {
                order.CardType = CardType.Plastic;
            }
            if (order.CardDesignType == 0)
            {
                order.CardDesignType = CardDesignType.BuiltInTemplate;
            }

            var pricing = await _unitOfWork.Repository<CardPricing>().GetQueryable()
                .AsNoTracking()
                .Where(p => p.CardType == order.CardType && p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
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

            var items = new List<CardOrderItem>();

            if (string.Equals(request.AssignmentScope, "specific_employees", StringComparison.OrdinalIgnoreCase))
            {
                if (request.EmployeeIds.Count != request.Quantity)
                    return ServiceResult<CardOrderDto>.Fail(
                        _messageService.Get("EmployeeCountMismatch", request.EmployeeIds.Count.ToString(), request.Quantity.ToString()), 422);

                var employees = await _unitOfWork.Repository<Employee>()
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(e => e.UserProfile)
                    .Where(e => request.EmployeeIds.Contains(e.Id))
                    .ToListAsync();

                var missingIds = request.EmployeeIds.Except(employees.Select(e => e.Id)).ToList();
                if (missingIds.Count > 0)
                    return ServiceResult<CardOrderDto>.Fail(
                        _messageService.Get("EmployeesNotFound", string.Join(", ", missingIds)), 422);

                var employeesWithoutProfile = employees.Where(e => e.UserProfile == null).Select(e => e.Id).ToList();
                if (employeesWithoutProfile.Count > 0)
                    return ServiceResult<CardOrderDto>.Fail(
                        _messageService.Get("EmployeesMissingProfile", string.Join(", ", employeesWithoutProfile)), 422);

                items = employees.Select(e => _mapper.Map<CardOrderItem>(e)).ToList();
            }
            else if (string.Equals(request.AssignmentScope, "all_employees", StringComparison.OrdinalIgnoreCase))
            {
                var allEmployees = await _unitOfWork.Repository<Employee>()
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(e => e.UserProfile)
                    .Where(e => !e.IsDeleted && e.UserProfile != null)
                    .ToListAsync();

                if (request.Quantity != allEmployees.Count)
                    return ServiceResult<CardOrderDto>.Fail(
                        _messageService.Get("EmployeeCountMismatch", allEmployees.Count.ToString(), request.Quantity.ToString()), 422);

                items = allEmployees.Select(e => _mapper.Map<CardOrderItem>(e)).ToList();
            }

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

            var reorder = new CardOrder
            {
                UserId = userId.Value,
                CardName = parentOrder.CardName,
                CardType = parentOrder.CardType,
                CardDesignType = parentOrder.CardDesignType,
                PrintTemplateId = parentOrder.PrintTemplateId,
                FrontDesignUrl = parentOrder.FrontDesignUrl,
                BackDesignUrl = parentOrder.BackDesignUrl,
                ParentOrderId = parentOrderId,
                Quantity = request.Quantity,
                Status = OrderStatus.PendingReview,
                UnitPrice = pricing.UnitPrice,
                Currency = pricing.Currency,
                TotalPrice = pricing.UnitPrice * request.Quantity,
                DeliveryMethod = request.DeliveryMethod,
                ShippingAddress = request.ShippingAddress,
                Items = items
            };

            await _unitOfWork.Repository<CardOrder>().AddAsync(reorder);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<CardOrderDto>.Success(
                _mapper.Map<CardOrderDto>(reorder),
                _messageService.Get("RecordCreated"));
        }

        public async Task<ServiceResult<CardOrderDto>> ReissueCardAsync(Guid cardId, ReissueCardRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
                return ServiceResult<CardOrderDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

            if (request.DeliveryMethod == DeliveryMethod.Courier && string.IsNullOrWhiteSpace(request.ShippingAddress))
            {
                return ServiceResult<CardOrderDto>.Fail(_messageService.Get("ShippingAddressRequired"), 422);
            }

            var card = await _unitOfWork.Repository<Card>()
                .GetQueryable()
                .Include(c => c.UserProfile)
                    .ThenInclude(up => up!.Employee)
                .Include(c => c.CardOrder)
                .FirstOrDefaultAsync(c => c.Id == cardId && !c.IsDeleted);

            if (card == null)
                return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("CardNotFound"));

            if (!card.UserProfileId.HasValue || card.UserProfile == null)
                return ServiceResult<CardOrderDto>.Fail(_messageService.Get("CardMustHaveProfileToReissue"), 422);

            // Deactivate the old card
            card.Status = CardStatus.Deactivated;

            var cardType = card.CardOrder?.CardType ?? CardType.Plastic;

            var pricing = await _unitOfWork.Repository<CardPricing>().GetQueryable()
                .AsNoTracking()
                .Where(p => p.CardType == cardType && p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (pricing == null)
            {
                return ServiceResult<CardOrderDto>.Fail(
                    _messageService.Get("PricingNotConfigured") ?? $"Pricing is not configured for card type '{cardType}'.",
                    500);
            }

            var reissueOrder = new CardOrder
            {
                UserId = userId.Value,
                CardName = card.CardOrder?.CardName ?? $"Replacement - {card.UserProfile.FullName}",
                CardType = cardType,
                CardDesignType = card.CardOrder?.CardDesignType ?? CardDesignType.BuiltInTemplate,
                PrintTemplateId = card.CardOrder?.PrintTemplateId,
                FrontDesignUrl = card.CardOrder?.FrontDesignUrl,
                BackDesignUrl = card.CardOrder?.BackDesignUrl,
                ParentOrderId = card.CardOrderId,
                Quantity = 1,
                Status = OrderStatus.PendingReview,
                UnitPrice = pricing.UnitPrice,
                Currency = pricing.Currency,
                TotalPrice = pricing.UnitPrice,
                DeliveryMethod = request.DeliveryMethod,
                ShippingAddress = request.ShippingAddress,
                Items = new List<CardOrderItem>
                {
                    _mapper.Map<CardOrderItem>(card.UserProfile)
                }
            };

            await _unitOfWork.Repository<CardOrder>().AddAsync(reissueOrder);
            await _unitOfWork.SaveChangesAsync();

            // Reload with items for the response
            var created = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == reissueOrder.Id);

            return ServiceResult<CardOrderDto>.Success(
                _mapper.Map<CardOrderDto>(created),
                _messageService.Get("RecordCreated"));
        }


        public async Task<ServiceResult<EmployeeImportJob>> QueueEmployeeImportJobAsync(
            Microsoft.AspNetCore.Http.IFormFile file,
            CardType cardType,
            CardDesignType cardDesignType,
            Guid? printTemplateId,
            string? notes)
        {
            var tenantId = _currentTenant.TenantId;
            var userId = _currentTenant.UserId;

            if (!tenantId.HasValue || !userId.HasValue)
                return ServiceResult<EmployeeImportJob>.Unauthorized(_messageService.Get("Unauthorized") ?? "User is not authenticated.");

            if (file == null || file.Length == 0)
                return ServiceResult<EmployeeImportJob>.Fail(_messageService.Get("NoFileUploaded") ?? "No file uploaded.", 400);

            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return ServiceResult<EmployeeImportJob>.Fail(_messageService.Get("ExcelFilesOnly") ?? "Only Excel files (.xlsx, .xls) are supported.", 400);

            // Upload Excel file to Cloudinary raw storage
            var uploadResult = await _storageService.UploadRawFileAsync(file, "employee-imports");
            if (string.IsNullOrWhiteSpace(uploadResult.SecureUrl))
            {
                return ServiceResult<EmployeeImportJob>.Fail(_messageService.Get("FileUploadFailed") ?? "Failed to upload the file to storage.", 500);
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
                PrintTemplateId = printTemplateId,
                Notes = notes
            };

            await _unitOfWork.Repository<EmployeeImportJob>().AddAsync(job);
            await _unitOfWork.SaveChangesAsync();

            // Enqueue Hangfire background job
            _backgroundJobClient.Enqueue<ICardOrderService>(s => s.ProcessEmployeeImportJobAsync(job.Id));

            return ServiceResult<EmployeeImportJob>.Success(job, _messageService.Get("RecordCreated") ?? "Import job queued successfully.");
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
                    var pattern = _messageService.Get("FailedToDownloadExcel") ?? "Failed to download Excel file from storage: {0}";
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
                    var pattern = _messageService.Get("FailedToParseExcel") ?? "Failed to parse Excel file: {0}";
                    errors.Add(string.Format(pattern, ex.Message));
                    await FailJobAsync(job, errors);
                    return;
                }

                if (employeeRows == null || employeeRows.Count == 0)
                {
                    errors.Add(_messageService.Get("NoValidEmployeeRows") ?? "No valid employee rows found in the uploaded file.");
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
                        var pattern = _messageService.Get("ImportRowNameRequired") ?? "Row {0}: Name is required.";
                        errors.Add(string.Format(pattern, rowNum));
                    }
                    if (string.IsNullOrWhiteSpace(row.Email))
                    {
                        var pattern = _messageService.Get("ImportRowEmailRequired") ?? "Row {0}: Email is required.";
                        errors.Add(string.Format(pattern, rowNum));
                    }
                    else
                    {
                        if (!emailRegex.IsMatch(row.Email))
                        {
                            var pattern = _messageService.Get("ImportRowEmailInvalid") ?? "Row {0}: Email '{1}' is not in a valid format.";
                            errors.Add(string.Format(pattern, rowNum, row.Email));
                        }
                        else if (!uniqueEmails.Add(row.Email))
                        {
                            var pattern = _messageService.Get("ImportRowEmailDuplicate") ?? "Row {0}: Duplicate email '{1}' found in the import sheet.";
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
                    errors.Add(_messageService.Get("CompanyNotFound") ?? "Company not found for this tenant.");
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
                    errors.Add(_messageService.Get("SubscriptionExpiredOrMissing") ?? "Active subscription missing or expired.");
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
                                throw new Exception("MaxEmployeesLimitReached");
                            }

                            // Create employee record
                            var newEmployee = _mapper.Map<Employee>(row);
                            newEmployee.CompanyId = company.Id;
                            newEmployee.TenantId = job.TenantId;

                            userProfile = _mapper.Map<UserProfile>(row);
                            userProfile.CompanyName = company.Name;
                            userProfile.TenantId = job.TenantId;

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
                        foreach (var emp in newEmployeesList)
                        {
                            await _unitOfWork.Repository<Employee>().AddAsync(emp);
                        }
                        foreach (var prof in newProfilesList)
                        {
                            await _unitOfWork.Repository<UserProfile>().AddAsync(prof);
                        }
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
                        PrintTemplateId = job.PrintTemplateId,
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
                        errors.Add(_messageService.Get("MaxEmployeesLimitReached") ?? "Max employee limit reached.");
                    }
                    else
                    {
                        var pattern = _messageService.Get("ImportExecutionFailed") ?? "Import execution failed: {0}";
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
                var errors = string.IsNullOrWhiteSpace(job.ErrorsJson)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(job.ErrorsJson) ?? new List<string>();

                var dto = new EmployeesImportStatusDto
                {
                    Status = job.Status.ToString(),
                    TotalRows = job.TotalRows,
                    Imported = job.Imported,
                    Skipped = job.Skipped,
                    Errors = errors
                };

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
                    _messageService.Get("RecordNotFound") ?? "Order/Job not found.");
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
    }
