namespace NFC.Platform.Application.Services;

    public class EmployeeService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant) : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public async Task<ServiceResult<PagedResult<EmployeeDto>>> GetPagedEmployeesAsync(PaginationRequest request, string? search)
        {
            var query = _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .AsNoTracking()
                .Include(e => e.UserProfile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.FullName.Contains(search) || 
                                         e.Email.Contains(search) || 
                                         e.JobTitle.Contains(search) || 
                                         e.Department.Contains(search));
            }

            var pagedResult = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToPagedResultAsync(request, e => _mapper.Map<EmployeeDto>(e));

            return ServiceResult<PagedResult<EmployeeDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> GetEmployeeDetailsAsync(Guid id)
        {
            var employee = await _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .AsNoTracking()
                .Include(e => e.UserProfile)
                    .ThenInclude(p => p!.CustomLinks)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(employee));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> CreateEmployeeAsync(CreateEmployeeRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<EmployeeDetailsDto>.Unauthorized(_messageService.Get("UserNotAuthenticated"));

            // 1. Fetch Company
            var company = await _unitOfWork.Repository<Company>().GetQueryable().AsNoTracking().FirstOrDefaultAsync();
            if (company == null)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("CompanyNotFound"), 400);

            // 2. Validate Subscription Limit
            var activeSub = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow);

            if (activeSub == null)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("SubscriptionExpiredOrMissing"), 400);

            var currentEmployeesCount = await _unitOfWork.Repository<Employee>()
                .CountAsync(e => e.TenantId == tenantId.Value && !e.IsDeleted);

            if (currentEmployeesCount >= activeSub.SubscriptionPlan.MaxEmployees)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("MaxEmployeesLimitReached"), 400);

            // 3. Unique check
            var existingEmployees = await _unitOfWork.Repository<Employee>().FindAsync(e => e.Email == request.Email && e.TenantId == tenantId.Value);
            if (existingEmployees.Count > 0)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("UserAlreadyExists"), 400);

            var employee = _mapper.Map<Employee>(request);
            employee.CompanyId = company.Id;

            var profile = _mapper.Map<UserProfile>(request);
            profile.EmployeeId = employee.Id;
            profile.CompanyName = company.Name;
            profile.TenantId = tenantId.Value;

            if (!string.IsNullOrWhiteSpace(request.CustomLinks))
            {
                profile.UpdateCustomLinks(request.CustomLinks);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<Employee>().AddAsync(employee);
                await _unitOfWork.Repository<UserProfile>().AddAsync(profile);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var dto = _mapper.Map<EmployeeDetailsDto>(employee);
            return ServiceResult<EmployeeDetailsDto>.Success(dto, _messageService.Get("RecordCreated"));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> UpdateEmployeeJobDetailsAsync(Guid id, UpdateEmployeeRequest request)
        {
            var employee = await _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .Include(e => e.UserProfile)
                    .ThenInclude(p => p!.CustomLinks)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            employee.Status = request.Status;
            employee.JobTitle = request.JobTitle ?? string.Empty;
            employee.Department = request.Department ?? string.Empty;
            employee.FullName = request.FullName ?? employee.FullName;

            if (employee.UserProfile != null)
            {
                _mapper.Map(request, employee.UserProfile);
                employee.UserProfile.UpdateCustomLinks(request.CustomLinks);
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(employee), _messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult> SoftDeleteEmployeeAsync(Guid id)
        {
            var employee = await _unitOfWork.Repository<Employee>().GetByIdAsync(id);
            if (employee == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            _unitOfWork.Repository<Employee>().Remove(employee);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RecordDeleted"));
        }
    }
