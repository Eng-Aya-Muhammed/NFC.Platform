namespace NFC.Platform.Application.Services;

public class EmployeeImportService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService,
    ICurrentTenant currentTenant,
    IExcelParser excelParser) : IEmployeeImportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
    private readonly IExcelParser _excelParser = excelParser ?? throw new ArgumentNullException(nameof(excelParser));

    public async Task ProcessImportJobAsync(Guid jobId)
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
            // 1. Get rows — use pre-parsed data if available, otherwise download & parse
            List<ExcelEmployeeImportDto> employeeRows;

            if (!string.IsNullOrWhiteSpace(job.PreParsedRowsJson))
            {
                employeeRows = System.Text.Json.JsonSerializer.Deserialize<List<ExcelEmployeeImportDto>>(job.PreParsedRowsJson)
                               ?? new List<ExcelEmployeeImportDto>();
            }
            else
            {
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

    public async Task<ServiceResult<EmployeesImportStatusDto>> GetImportStatusAsync(Guid orderId)
    {
        // 1. Try to find the import job first
        var job = await _unitOfWork.Repository<EmployeeImportJob>()
            .GetQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == orderId);

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
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return ServiceResult<EmployeesImportStatusDto>.NotFound(
                _messageService.Get("RecordNotFound"));
        }

        var orderDto = _mapper.Map<EmployeesImportStatusDto>(order);
        orderDto.Status = "Completed";

        return ServiceResult<EmployeesImportStatusDto>.Success(orderDto);
    }
}
