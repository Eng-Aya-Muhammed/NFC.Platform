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
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ICurrentTenant currentTenant)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        }

        public async Task<ServiceResult<PagedResult<EmployeeDto>>> GetPagedEmployeesAsync(PaginationRequest request, string? search)
        {
            var query = _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                .Where(u => u.AccountType == AccountType.Employee);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Username.Contains(search) || 
                                         u.Email.Contains(search) || 
                                         (u.UserProfile != null && u.UserProfile.FullName.Contains(search)));
            }

            var pagedResult = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToPagedResultAsync(request, u => _mapper.Map<EmployeeDto>(u));

            return ServiceResult<PagedResult<EmployeeDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> GetEmployeeDetailsAsync(Guid id)
        {
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == id && u.AccountType == AccountType.Employee);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user));
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

            var currentEmployeesCount = await _unitOfWork.Repository<User>()
                .CountAsync(u => u.TenantId == tenantId.Value && u.AccountType == AccountType.Employee && !u.IsDeleted);

            if (currentEmployeesCount >= activeSub.SubscriptionPlan.MaxEmployees)
                return ServiceResult<EmployeeDetailsDto>.Fail("MaxEmployeesLimitReached", 400);

            // 3. Unique check
            var existingUsers = await _unitOfWork.Repository<User>().FindAsync(u => u.Email == request.Email || u.Username == request.Username);
            if (existingUsers.Count > 0)
                return ServiceResult<EmployeeDetailsDto>.Fail(_messageService.Get("UserAlreadyExists"), 400);

            // 4. Generate Temp Password
            var tempPassword = GenerateTemporaryPassword();

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = PasswordHasher.HashPassword(tempPassword),
                AccountType = AccountType.Employee,
                TenantId = tenantId.Value,
                CompanyId = company.Id,
                Status = UserStatus.Active
            };

            var profile = new UserProfile
            {
                UserId = user.Id,
                TenantId = tenantId.Value,
                FullName = request.FullName,
                JobTitle = request.JobTitle ?? string.Empty,
                Department = request.Department ?? string.Empty,
                CompanyName = company.Name
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<User>().AddAsync(user);
                await _unitOfWork.Repository<UserProfile>().AddAsync(profile);
                await _unitOfWork.SaveChangesAsync();

                // Assign role
                var roles = await _unitOfWork.Repository<Role>().FindAsync(r => r.Name == AppRole.Employee.ToString());
                var employeeRole = roles.Count > 0 ? roles[0] : null;
                if (employeeRole != null)
                {
                    await _unitOfWork.Repository<UserRole>().AddAsync(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = employeeRole.Id
                    });
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var dto = _mapper.Map<EmployeeDetailsDto>(user);
            dto.TemporaryPassword = tempPassword;

            return ServiceResult<EmployeeDetailsDto>.Success(dto, _messageService.Get("RecordCreated"));
        }

        public async Task<ServiceResult<EmployeeDetailsDto>> UpdateEmployeeJobDetailsAsync(Guid id, UpdateEmployeeRequest request)
        {
            var user = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == id && u.AccountType == AccountType.Employee);

            if (user == null)
                return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

            user.Status = request.Status;

            if (user.UserProfile != null)
            {
                user.UserProfile.JobTitle = request.JobTitle ?? string.Empty;
                user.UserProfile.Department = request.Department ?? string.Empty;
                _unitOfWork.Repository<UserProfile>().Update(user.UserProfile);
            }

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<EmployeeDetailsDto>.Success(_mapper.Map<EmployeeDetailsDto>(user), _messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult> SoftDeleteEmployeeAsync(Guid id)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null || user.AccountType != AccountType.Employee)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            _unitOfWork.Repository<User>().Remove(user);
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

        private static string GenerateTemporaryPassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$";
            var randomBytes = RandomNumberGenerator.GetBytes(8);
            var password = new char[8];
            for (int i = 0; i < 8; i++)
            {
                password[i] = validChars[randomBytes[i] % validChars.Length];
            }
            return new string(password);
        }
    }
}
