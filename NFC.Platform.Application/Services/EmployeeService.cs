using System.Net.Http;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Constants;

using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;

namespace NFC.Platform.Application.Services;

    public class EmployeeService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant,
        IExcelParser excelParser,
        IHttpClientFactory httpClientFactory,
        ExportBuilder? exportBuilder = null,
        IExcelExportService? excelExportService = null,
        IPdfExportService? pdfExportService = null) : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        private readonly IExcelParser _excelParser = excelParser ?? throw new ArgumentNullException(nameof(excelParser));
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        private readonly ExportBuilder? _exportBuilder = exportBuilder;
        private readonly IExcelExportService? _excelExportService = excelExportService;
        private readonly IPdfExportService? _pdfExportService = pdfExportService;

        public async Task<ServiceResult<PagedResult<EmployeeDto>>> GetPagedEmployeesAsync(PaginationRequest request, string? search)
        {
            var query = _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .AsNoTracking()
                .Include(e => e.UserProfile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e => (e.FullName != null && e.FullName.Contains(search)) || 
                                         (e.Email != null && e.Email.Contains(search)) || 
                                         (e.JobTitle != null && e.JobTitle.Contains(search)) || 
                                         (e.Department != null && e.Department.Contains(search)) ||
                                         (e.UserProfile != null && (
                                             (e.UserProfile.Phone != null && e.UserProfile.Phone.Contains(search)) ||
                                             (e.UserProfile.Subdomain != null && e.UserProfile.Subdomain.Contains(search))
                                         )));
            }

            var pagedResult = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToPagedResultAsync(request, e => _mapper.Map<EmployeeDto>(e));

            int? daysRemaining = null;
            if (_currentTenant.TenantId.HasValue)
            {
                var subscription = await _unitOfWork.Repository<NFC.Platform.Domain.Entities.UserSubscription>()
                    .GetQueryable()
                    .Where(s => s.TenantId == _currentTenant.TenantId.Value && s.IsActive)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();
                
                if (subscription != null)
                {
                    daysRemaining = (subscription.EndDate.Date - DateTime.UtcNow.Date).Days;
                    if (daysRemaining < 0) daysRemaining = 0;
                }
            }

            if (daysRemaining.HasValue)
            {
                foreach (var item in pagedResult.Items)
                {
                    item.SubscriptionDaysRemaining = daysRemaining.Value;
                }
            }

            return ServiceResult<PagedResult<EmployeeDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult<byte[]>> ExportEmployeesAsync(ExportFormat format, string? search)
        {
            if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
            {
                return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
            }

            var query = _unitOfWork.Repository<Employee>()
                .GetQueryable()
                .AsNoTracking()
                .Include(e => e.UserProfile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e => (e.FullName != null && e.FullName.Contains(search)) || 
                                         (e.Email != null && e.Email.Contains(search)) || 
                                         (e.JobTitle != null && e.JobTitle.Contains(search)) || 
                                         (e.Department != null && e.Department.Contains(search)) ||
                                         (e.UserProfile != null && (
                                             (e.UserProfile.Phone != null && e.UserProfile.Phone.Contains(search)) ||
                                             (e.UserProfile.Subdomain != null && e.UserProfile.Subdomain.Contains(search))
                                         )));
            }

            var employees = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var exportDtos = _mapper.Map<List<EmployeeExportDto>>(employees);
            var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_Employees");

            byte[] fileBytes = format switch
            {
                ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
                ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };

            return ServiceResult<byte[]>.Success(fileBytes);
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

            var company = await GetCompanyOrThrowAsync();
            if (company == null)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("CompanyNotFound"), 400);

            var subscriptionError = await ValidateSubscriptionQuotaAsync(tenantId.Value);
            if (subscriptionError != null)
                return ServiceResult<EmployeeDetailsDto>.Fail(subscriptionError, 400);

            var emailError = await EnsureEmailIsUniqueAsync(request.Email, tenantId.Value);
            if (emailError != null)
                return ServiceResult<EmployeeDetailsDto>.Fail(emailError, 400);

            var (employee, profile) = await BuildEmployeeEntitiesAsync(request, company.Id, company.Name, tenantId.Value);

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

            _mapper.Map(request, employee);

            if (employee.UserProfile != null)
            {
                _mapper.Map(request, employee.UserProfile);
                employee.UserProfile.UpdateCustomLinks(request.Links);
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

        public async Task<ServiceResult<List<Guid>>> UpsertEmployeesFromExcelAsync(string excelUrl, Guid companyId, Guid tenantId)
        {
            var parseResult = await DownloadAndParseExcelAsync(excelUrl);
            if (!parseResult.IsSuccess)
                return ServiceResult<List<Guid>>.Fail(parseResult.Message ?? string.Join(", ", parseResult.Errors), parseResult.StatusCode);

            var rows = parseResult.Data!;

            var validationResult = ValidateExcelRows(rows);
            if (!validationResult.IsSuccess)
                return validationResult;

            return await ProcessEmployeeRowsAsync(rows, companyId, tenantId);
        }

        private async Task<ServiceResult<List<ExcelEmployeeImportDto>>> DownloadAndParseExcelAsync(string excelUrl)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var fileBytes = await httpClient.GetByteArrayAsync(excelUrl);
                using var stream = new System.IO.MemoryStream(fileBytes);
                var rows = _excelParser.ParseEmployeesFromExcel(stream);
                if (rows == null || rows.Count == 0)
                    return ServiceResult<List<ExcelEmployeeImportDto>>.Fail(_messageService.Get("NoValidEmployeeRows"), 422);

                return ServiceResult<List<ExcelEmployeeImportDto>>.Success(rows);
            }
            catch (HttpRequestException)
            {
                return ServiceResult<List<ExcelEmployeeImportDto>>.Fail(_messageService.Get("FailedToDownloadExcel"), 422);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ExcelEmployeeImportDto>>.Fail($"{_messageService.Get("FailedToParseExcel")} - {ex.Message}", 422);
            }
        }

        private ServiceResult<List<Guid>> ValidateExcelRows(List<ExcelEmployeeImportDto> rows)
        {
            var errors = new List<string>();
            var emailRegex = new System.Text.RegularExpressions.Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var uniqueEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNum = i + 1;

                if (string.IsNullOrWhiteSpace(row.Name))
                    errors.Add(_messageService.Get("ImportRowNameRequired", rowNum.ToString()));

                if (string.IsNullOrWhiteSpace(row.Email))
                    errors.Add(_messageService.Get("ImportRowEmailRequired", rowNum.ToString()));
                else if (!emailRegex.IsMatch(row.Email))
                    errors.Add(_messageService.Get("ImportRowEmailInvalid", rowNum.ToString(), row.Email));
                else if (!uniqueEmails.Add(row.Email))
                    errors.Add(_messageService.Get("ImportRowEmailDuplicate", rowNum.ToString(), row.Email));
            }

            if (errors.Count > 0)
                return ServiceResult<List<Guid>>.Fail(errors, 422);

            return ServiceResult<List<Guid>>.Success([]);
        }

        private async Task<ServiceResult<List<Guid>>> ProcessEmployeeRowsAsync(List<ExcelEmployeeImportDto> rows, Guid companyId, Guid tenantId)
        {
            var context = await GetImportContextAsync(rows, companyId, tenantId);

            if (context.ActiveSub == null)
                return ServiceResult<List<Guid>>.Fail(_messageService.Get("SubscriptionExpiredOrMissing"), 422);

            var newEmployeesList = new List<Employee>();
            var newProfilesList = new List<UserProfile>();
            var newEmployeesCount = 0;
            var resultIds = new List<Guid>();
            var localCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (context.EmployeesByEmail.TryGetValue(row.Email, out var existingEmployee))
                {
                    UpdateExistingEmployee(existingEmployee, row);
                    resultIds.Add(existingEmployee.Id);
                }
                else
                {


                    var (newEmployee, userProfile) = await CreateNewEmployeeAsync(row, companyId, tenantId, context.CompanyName, localCache);

                    newEmployeesList.Add(newEmployee);
                    newProfilesList.Add(userProfile);
                    newEmployeesCount++;
                    
                    context.EmployeesByEmail[row.Email] = newEmployee;
                    resultIds.Add(newEmployee.Id);
                }
            }

            if (newEmployeesList.Count > 0)
            {
                await _unitOfWork.Repository<Employee>().AddRangeAsync(newEmployeesList);
                await _unitOfWork.Repository<UserProfile>().AddRangeAsync(newProfilesList);
            }

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<List<Guid>>.Success(resultIds);
        }

        private async Task<(UserSubscription? ActiveSub, int CurrentEmployeesCount, Dictionary<string, Employee> EmployeesByEmail, string CompanyName)> GetImportContextAsync(List<ExcelEmployeeImportDto> rows, Guid companyId, Guid tenantId)
        {
            var activeSub = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive && s.EndDate >= DateTime.UtcNow);

            var currentEmployeesCount = await _unitOfWork.Repository<Employee>()
                .CountAsync(e => e.TenantId == tenantId && !e.IsDeleted);

            var targetEmails = rows.Select(r => r.Email).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existingEmployees = new List<Employee>();
            
            foreach (var emailChunk in targetEmails.Chunk(1000))
            {
                var chunkResult = await _unitOfWork.Repository<Employee>()
                    .GetQueryable()
                    .Include(e => e.UserProfile)
                    .Where(e => e.TenantId == tenantId && !e.IsDeleted && emailChunk.Contains(e.Email))
                    .ToListAsync();
                existingEmployees.AddRange(chunkResult);
            }

            var employeesByEmail = existingEmployees.ToDictionary(e => e.Email, e => e, StringComparer.OrdinalIgnoreCase);

            var company = await _unitOfWork.Repository<Company>().GetQueryable().AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
            var companyName = company?.Name ?? string.Empty;

            return (activeSub, currentEmployeesCount, employeesByEmail, companyName);
        }


        private static void UpdateExistingEmployee(Employee existingEmployee, ExcelEmployeeImportDto row)
        {
            existingEmployee.FullName = row.Name;
            existingEmployee.JobTitle = row.JobTitle ?? string.Empty;
            existingEmployee.Department = row.Department ?? string.Empty;

            if (existingEmployee.UserProfile != null)
            {
                existingEmployee.UserProfile.FullName = row.Name;
                existingEmployee.UserProfile.JobTitle = row.JobTitle ?? string.Empty;
                existingEmployee.UserProfile.Department = row.Department;
                existingEmployee.UserProfile.Phone = row.Phone;
                if (!string.IsNullOrWhiteSpace(row.WhatsApp))
                {
                    existingEmployee.UserProfile.WhatsApp = row.WhatsApp;
                }
                if (row.CustomLinks != null && row.CustomLinks.Count > 0)
                {
                    existingEmployee.UserProfile.UpdateCustomLinks(row.CustomLinks);
                }
            }
        }

        private async Task<(Employee Employee, UserProfile Profile)> CreateNewEmployeeAsync(ExcelEmployeeImportDto row, Guid companyId, Guid tenantId, string companyName, HashSet<string> localCache)
        {
            var newEmployee = _mapper.Map<Employee>(row);
            newEmployee.Id = Guid.NewGuid();
            newEmployee.CompanyId = companyId;
            newEmployee.TenantId = tenantId;

            var userProfile = _mapper.Map<UserProfile>(row);
            userProfile.Id = Guid.NewGuid();
            userProfile.CompanyName = companyName;
            userProfile.TenantId = tenantId;
            if (!string.IsNullOrWhiteSpace(row.WhatsApp))
            {
                userProfile.WhatsApp = row.WhatsApp;
            }
            if (row.CustomLinks != null && row.CustomLinks.Count > 0)
            {
                userProfile.UpdateCustomLinks(row.CustomLinks);
            }

            // Generate unique slug — checked against DB AND the current batch (localCache)
            // to prevent intra-batch collisions before the transaction commits.
            var baseSlug = SubdomainHelper.Slugify(row.Name);
            var candidate = baseSlug;
            var profileRepo = _unitOfWork.Repository<UserProfile>();
            while (localCache.Contains(candidate) ||
                   (await profileRepo.GetQueryable().IgnoreQueryFilters().AnyAsync(p => p.Subdomain == candidate)))
            {
                candidate = $"{baseSlug}-{Random.Shared.Next(1000, 9999)}";
            }
            userProfile.Subdomain = candidate;
            localCache.Add(candidate);

            newEmployee.UserProfile = userProfile;
            userProfile.Employee = newEmployee;

            return (newEmployee, userProfile);
        }

        private async Task<Company?> GetCompanyOrThrowAsync()
        {
            return await _unitOfWork.Repository<Company>().GetQueryable().AsNoTracking().FirstOrDefaultAsync();
        }

        private async Task<string?> ValidateSubscriptionQuotaAsync(Guid tenantId)
        {
            var activeSub = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive && s.EndDate >= DateTime.UtcNow);

            if (activeSub == null)
                return _messageService.Get("SubscriptionExpiredOrMissing");

            return null; 
        }

        private async Task<string?> EnsureEmailIsUniqueAsync(string email, Guid tenantId)
        {
            var exists = await _unitOfWork.Repository<Employee>().GetQueryable().AnyAsync(e => e.Email == email && e.TenantId == tenantId);
            if (exists)
                return _messageService.Get("UserAlreadyExists");

            return null;
        }

        private async Task<(Employee Employee, UserProfile Profile)> BuildEmployeeEntitiesAsync(CreateEmployeeRequest request, Guid companyId, string companyName, Guid tenantId)
        {
            var employee = _mapper.Map<Employee>(request);
            employee.CompanyId = companyId;

            var profile = _mapper.Map<UserProfile>(request);
            profile.EmployeeId = employee.Id;
            profile.CompanyName = companyName;
            profile.TenantId = tenantId;

            // Generate unique public slug for this employee profile
            var baseSlug = SubdomainHelper.Slugify(request.FullName);
            profile.Subdomain = await GenerateUniqueSubdomainAsync(baseSlug);

            if (request.Links?.Count > 0)
            {
                profile.UpdateCustomLinks(request.Links);
            }

            return (employee, profile);
        }

        /// <summary>
        /// Generates a unique subdomain slug. Checks the DB for existing slugs and
        /// appends a 4-digit random suffix on collision.
        /// </summary>
        private async Task<string> GenerateUniqueSubdomainAsync(string baseSlug)
        {
            var candidate = baseSlug;
            var profileRepo = _unitOfWork.Repository<UserProfile>();

            while (true)
            {
                var query = profileRepo.GetQueryable();
                bool taken;

                if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
                    taken = await query.IgnoreQueryFilters().AnyAsync(p => p.Subdomain == candidate);
                else
                    taken = (await profileRepo.FindAsync(p => p.Subdomain == candidate)).Count > 0;

                if (!taken) return candidate;
                candidate = $"{baseSlug}-{Random.Shared.Next(1000, 9999)}";
            }
        }
    }

