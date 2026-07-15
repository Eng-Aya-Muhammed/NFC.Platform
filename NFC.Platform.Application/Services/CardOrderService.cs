namespace NFC.Platform.Application.Services;

    public class CardOrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant,
        IExcelParser excelParser,
        FluentValidation.IValidator<CreateCardOrderRequest> validator) : ICardOrderService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        private readonly IExcelParser _excelParser = excelParser ?? throw new ArgumentNullException(nameof(excelParser));
        private readonly FluentValidation.IValidator<CreateCardOrderRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

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

        public Task<ServiceResult<OrderPricingResponseDto>> GetOrderPricingAsync(string cardType, int quantity)
        {
            // Config-based pricing tiers. Can be moved to a DB table later.
            decimal unitPrice = cardType?.ToLower() switch
            {
                "metal" => 8.500m,
                "wood"  => 6.000m,
                _       => 4.500m   // plastic (default)
            };

            var dto = new OrderPricingResponseDto
            {
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * quantity,
                Currency = "KWD"
            };

            return Task.FromResult(ServiceResult<OrderPricingResponseDto>.Success(dto));
        }

        public async Task<ServiceResult<CardOrderDto>> CreateReorderAsync(Guid parentOrderId, ReorderRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
                return ServiceResult<CardOrderDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

            var parentOrder = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == parentOrderId);

            if (parentOrder == null)
                return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

            // Validate specific_employees scope
            if (string.Equals(request.AssignmentScope, "specific_employees", StringComparison.OrdinalIgnoreCase))
            {
                if (request.EmployeeIds.Count != request.Quantity)
                    return ServiceResult<CardOrderDto>.Fail(
                        _messageService.Get("EmployeeCountMismatch", request.EmployeeIds.Count.ToString(), request.Quantity.ToString()), 422);
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
                Status = OrderStatus.PendingReview
            };

            await _unitOfWork.Repository<CardOrder>().AddAsync(reorder);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<CardOrderDto>.Success(
                _mapper.Map<CardOrderDto>(reorder),
                _messageService.Get("RecordCreated"));
        }


        public async Task<ServiceResult<CardOrderDto>> ImportEmployeesAndCreateBulkOrderAsync(CreateBulkCardOrderFromExcelRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var tenantId = _currentTenant.TenantId;
            var userId = _currentTenant.UserId;

            if (!tenantId.HasValue || !userId.HasValue)
                return ServiceResult<CardOrderDto>.Unauthorized("User is not authenticated.");

            // 1. Fetch Company info for this tenant
            var company = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId.Value);

            if (company == null)
                return ServiceResult<CardOrderDto>.Fail("Company not found for this tenant.", 400);

            // 2. Fetch Active Subscription and Max employee limits
            var activeSub = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow);

            if (activeSub == null)
                return ServiceResult<CardOrderDto>.Fail("SubscriptionExpiredOrMissing", 400);

            var currentEmployeesCount = await _unitOfWork.Repository<Employee>()
                .CountAsync(e => e.TenantId == tenantId.Value && !e.IsDeleted);

            // 3. Parse Excel file into rows
            List<ExcelEmployeeImportDto> employeeRows;
            try
            {
                employeeRows = _excelParser.ParseEmployeesFromExcel(request.ExcelStream);
            }
            catch (Exception ex)
            {
                var pattern = _messageService.Get("FailedToParseExcel") ?? "Failed to parse Excel file: {0}";
                return ServiceResult<CardOrderDto>.Fail(string.Format(pattern, ex.Message), 400);
            }

            if (employeeRows == null || employeeRows.Count == 0)
                return ServiceResult<CardOrderDto>.Fail(_messageService.Get("NoValidEmployeeRows") ?? "No valid employee rows found in the uploaded file.", 400);

            // 4. Batch queries to prevent N+1 queries in loop
            var existingUserProfilesList = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .AsNoTracking()
                .Include(u => u.UserProfile)
                .Where(u => u.TenantId == tenantId.Value)
                .ToListAsync();

            var userProfilesByEmail = existingUserProfilesList
                .Where(u => u.UserProfile != null)
                .ToDictionary(u => u.Email, u => u.UserProfile!, StringComparer.OrdinalIgnoreCase);

            var employeesByEmail = await _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .Include(e => e.UserProfile)
                .Where(e => e.TenantId == tenantId.Value && !e.IsDeleted)
                .ToDictionaryAsync(e => e.Email, e => e, StringComparer.OrdinalIgnoreCase);

            var itemsToOrder = new List<CardOrderItem>();
            var newEmployeesCount = 0;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
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
                            await _unitOfWork.RollbackTransactionAsync();
                            return ServiceResult<CardOrderDto>.Fail("MaxEmployeesLimitReached", 400);
                        }

                        // Create employee record (no login credentials generated)
                        var newEmployee = _mapper.Map<Employee>(row);
                        newEmployee.CompanyId = company.Id;
                        newEmployee.TenantId = tenantId.Value;

                        await _unitOfWork.Repository<Employee>().AddAsync(newEmployee);
                        await _unitOfWork.SaveChangesAsync();

                        userProfile = _mapper.Map<UserProfile>(row);
                        userProfile.CompanyName = company.Name;
                        userProfile.EmployeeId = newEmployee.Id;
                        userProfile.TenantId = tenantId.Value;

                        await _unitOfWork.Repository<UserProfile>().AddAsync(userProfile);
                        await _unitOfWork.SaveChangesAsync();

                        newEmployeesCount++;

                        newEmployee.UserProfile = userProfile;
                        employeesByEmail[row.Email] = newEmployee;
                    }

                    if (userProfile != null)
                    {
                        var orderItem = _mapper.Map<CardOrderItem>(row);
                        orderItem.UserProfileId = userProfile.Id;
                        orderItem.TenantId = tenantId.Value;
                        itemsToOrder.Add(orderItem);
                    }
                }

                var cardOrder = new CardOrder
                {
                    CardName = _messageService.Get("DefaultBulkOrderName", itemsToOrder.Count.ToString()) ?? $"Bulk Order - {itemsToOrder.Count}",
                    CardType = request.CardType,
                    CardDesignType = request.CardDesignType,
                    PrintTemplateId = request.PrintTemplateId,
                    Quantity = itemsToOrder.Count,
                    Notes = request.Notes,
                    UserId = userId.Value,
                    TenantId = tenantId.Value,
                    Status = OrderStatus.PendingReview,
                    Items = itemsToOrder
                };

                await _unitOfWork.Repository<CardOrder>().AddAsync(cardOrder);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                // Reload bulk order with items
                var created = await _unitOfWork.Repository<CardOrder>()
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == cardOrder.Id);

                return ServiceResult<CardOrderDto>.Success(
                    _mapper.Map<CardOrderDto>(created),
                    _messageService.Get("RecordCreated"));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ServiceResult<EmployeesImportStatusDto>> GetEmployeesImportStatusAsync(Guid orderId)
        {
            var order = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return ServiceResult<EmployeesImportStatusDto>.NotFound(
                    _messageService.Get("RecordNotFound") ?? "Order not found.");
            }

            var dto = _mapper.Map<EmployeesImportStatusDto>(order);
            dto.Status = _messageService.Get("ImportCompleted");

            return ServiceResult<EmployeesImportStatusDto>.Success(dto);
        }
    }
