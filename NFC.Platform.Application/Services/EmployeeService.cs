using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Services
{
    public class EmployeeService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant) : IEmployeeService
    {
        private static readonly string[] LineSeparators = ["\r\n", "\r", "\n"];

        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public async Task<ServiceResult<PagedResult<EmployeeDto>>> GetPagedEmployeesAsync(PaginationRequest request, string? search)
        {
            var query = _unitOfWork.Repository<Employee>()
                .GetQueryable()
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
                .Include(e => e.UserProfile)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(employee));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> CreateEmployeeAsync(CreateEmployeeRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<EmployeeDetailsDto>.Unauthorized("User is not authenticated.");

            // 1. Fetch Company
            var company = await _unitOfWork.Repository<Company>().GetQueryable().FirstOrDefaultAsync();
            if (company == null)
                return ServiceResult<EmployeeDetailsDto>.Fail("Company not found for this tenant.", 400);

            // 2. Validate Subscription Limit
            var activeSub = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value && s.IsActive && s.EndDate >= DateTime.UtcNow);

            if (activeSub == null)
                return ServiceResult<EmployeeDetailsDto>.Fail("SubscriptionExpiredOrMissing", 400);

            var currentEmployeesCount = await _unitOfWork.Repository<Employee>()
                .CountAsync(e => e.TenantId == tenantId.Value && !e.IsDeleted);

            if (currentEmployeesCount >= activeSub.SubscriptionPlan.MaxEmployees)
                return ServiceResult<EmployeeDetailsDto>.Fail("MaxEmployeesLimitReached", 400);

            // 3. Unique check
            var existingEmployees = await _unitOfWork.Repository<Employee>().FindAsync(e => e.Email == request.Email && e.TenantId == tenantId.Value);
            if (existingEmployees.Count > 0)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("UserAlreadyExists"), 400);

            var employee = new Employee
            {
                FullName = request.FullName,
                Email = request.Email,
                JobTitle = request.JobTitle ?? string.Empty,
                Department = request.Department ?? string.Empty,
                TenantId = tenantId.Value,
                CompanyId = company.Id,
                Status = UserStatus.Active
            };

            var profile = new UserProfile
            {
                EmployeeId = employee.Id,
                TenantId = tenantId.Value,
                FullName = request.FullName,
                JobTitle = request.JobTitle ?? string.Empty,
                Department = request.Department ?? string.Empty,
                CompanyName = company.Name,
                ProfilePictureUrl = request.ProfilePictureUrl,
                Phone = request.Phone,
                WhatsApp = request.WhatsApp,
                InstagramUrl = request.InstagramUrl,
                FacebookUrl = request.FacebookUrl,
                LinkedInUrl = request.LinkedInUrl,
                WebsiteUrl = request.WebsiteUrl,
                ContactEmail = request.Email
            };

            if (!string.IsNullOrWhiteSpace(request.CustomLinks))
            {
                var lines = request.CustomLinks.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
                var displayOrder = 1;
                foreach (var line in lines)
                {
                    var url = line.Trim();
                    profile.CustomLinks.Add(new ProfileLink
                    {
                        TenantId = tenantId.Value,
                        Title = url,
                        Url = url,
                        DisplayOrder = displayOrder++
                    });
                }
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
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            employee.Status = request.Status;
            employee.JobTitle = request.JobTitle ?? string.Empty;
            employee.Department = request.Department ?? string.Empty;

            if (employee.UserProfile != null)
            {
                employee.UserProfile.JobTitle = request.JobTitle ?? string.Empty;
                employee.UserProfile.Department = request.Department ?? string.Empty;
                _unitOfWork.Repository<UserProfile>().Update(employee.UserProfile);
            }

            _unitOfWork.Repository<Employee>().Update(employee);
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

        public async Task<ServiceResult<EmployeeDetailsDto>> GetMyProfileAsync(Guid userId)
        {
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> UpdateMyProfileAsync(Guid userId, UpdateMyProfileRequest request)
        {
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = userId, TenantId = user.TenantId };
                await _unitOfWork.Repository<UserProfile>().AddAsync(user.UserProfile);
                await _unitOfWork.SaveChangesAsync();
            }

            _mapper.Map(request, user.UserProfile);
            _unitOfWork.Repository<UserProfile>().Update(user.UserProfile);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user), _messageService.Get("RecordUpdated"));
        }
    }
}
