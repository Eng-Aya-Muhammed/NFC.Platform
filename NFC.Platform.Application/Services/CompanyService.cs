using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        public CompanyService(
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

        public async Task<ServiceResult<CompanyProfileDto>> GetMyCompanyProfileAsync()
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<CompanyProfileDto>.Unauthorized("User is not authenticated.");

            // Fetch the single company associated with the tenant
            var company = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .Include(c => c.AdminUser)
                .FirstOrDefaultAsync();

            if (company == null)
                return ServiceResult<CompanyProfileDto>.NotFound(_messageService.Get("RecordNotFound"));

            var remainingDays = await GetSubscriptionRemainingDaysAsync(tenantId.Value);

            var companyDto = _mapper.Map<CompanyProfileDto>(company);
            companyDto.SubscriptionRemainingDays = remainingDays;

            return ServiceResult<CompanyProfileDto>.Success(companyDto);
        }

        public async Task<ServiceResult<CompanyProfileDto>> UpdateCompanyProfileAsync(UpdateCompanyProfileRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
                return ServiceResult<CompanyProfileDto>.Unauthorized("User is not authenticated.");

            var company = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .Include(c => c.AdminUser)
                .FirstOrDefaultAsync();

            if (company == null)
                return ServiceResult<CompanyProfileDto>.NotFound(_messageService.Get("RecordNotFound"));

            _mapper.Map(request, company);
            _unitOfWork.Repository<Company>().Update(company);
            await _unitOfWork.SaveChangesAsync();

            var remainingDays = await GetSubscriptionRemainingDaysAsync(tenantId.Value);

            var companyDto = _mapper.Map<CompanyProfileDto>(company);
            companyDto.SubscriptionRemainingDays = remainingDays;

            return ServiceResult<CompanyProfileDto>.Success(companyDto, _messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult> ChangeCompanyAdminPasswordAsync(CompanyChangePasswordRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
                return ServiceResult.Unauthorized("User is not authenticated.");

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId.Value);
            if (user == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            if (!PasswordHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
            {
                return ServiceResult.Fail(_messageService.Get("InvalidCredentials"), 400);
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("PasswordResetSuccess"));
        }

        private async Task<int> GetSubscriptionRemainingDaysAsync(Guid tenantId)
        {
            var subscription = await _unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive && s.EndDate >= DateTime.UtcNow);

            if (subscription == null)
                return 0;

            var remaining = (subscription.EndDate - DateTime.UtcNow).Days;
            return remaining < 0 ? 0 : remaining;
        }
    }
}
