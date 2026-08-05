using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.BuildingBlocks.Common.Models;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Interfaces.Services;

public interface IAdminService
{
    Task<ServiceResult<PagedResult<AdminOrderSummaryDto>>> GetOrdersPagedAsync(PaginationRequest request, OrderStatus? statusFilter, Guid? companyId = null, Guid? tenantId = null, string? search = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<AdminOrderDetailDto>> GetOrderByIdAsync(Guid id);
    Task<ServiceResult> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto);
    Task<ServiceResult> VerifyDeliveryOtpAsync(Guid orderId, string otp);
    Task<ServiceResult> ResendDeliveryOtpAsync(Guid orderId);
    Task<ServiceResult<PagedResult<TemplateRequestDto>>> GetTemplateRequestsPagedAsync(PaginationRequest request, TemplateRequestStatus? status = null, string? search = null, CancellationToken cancellationToken = default);
    Task<ServiceResult> ResolveTemplateRequestAsync(Guid id, ResolveTemplateRequestDto dto);
    Task<ServiceResult<CardTemplateAdminDto>> CreateTemplateAsync(CreateCardTemplateRequest dto);
    Task<ServiceResult<CardTemplateAdminDto>> UpdateTemplateAsync(Guid id, UpdateCardTemplateRequest dto);
    Task<ServiceResult> DeleteTemplateAsync(Guid id);
    Task<ServiceResult<PagedResult<TenantSummaryDto>>> GetTenantsPagedAsync(PaginationRequest request, string? search = null, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateTenantStatusAsync(Guid id, UpdateTenantStatusDto dto);
    
    // Admin Tenant/Company Details
    Task<ServiceResult<TenantBasicInfoDto>> GetTenantBasicInfoAsync(Guid tenantId);
    Task<ServiceResult<PagedResult<EmployeeDto>>> GetTenantEmployeesPagedAsync(Guid tenantId, PaginationRequest request, string? search = null);
    Task<ServiceResult<EmployeeDetailsDto>> GetTenantEmployeeDetailsAsync(Guid tenantId, Guid employeeId);

    // Subscription Plan Management (Super Admin)
    Task<ServiceResult<PagedResult<SubscriptionPlanAdminDto>>> GetAllAdminPlansAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<SubscriptionPlanAdminDto>> GetPlanByIdAsync(Guid id);
    Task<ServiceResult<SubscriptionPlanAdminDto>> CreatePlanAsync(CreateSubscriptionPlanRequest request);
    Task<ServiceResult<SubscriptionPlanAdminDto>> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanRequest request);
    Task<ServiceResult> DeletePlanAsync(Guid planId);

    // Plan Template Assignment (Super Admin)
    Task<ServiceResult<IReadOnlyList<CardTemplateSummaryDto>>> GetPlanTemplatesAsync(Guid planId);
    Task<ServiceResult> AssignTemplateAsync(Guid planId, Guid templateId);
    Task<ServiceResult> UnassignTemplateAsync(Guid planId, Guid templateId);

    Task<ServiceResult<byte[]>> ExportAdminOrdersAsync(ExportFormat format, OrderStatus? statusFilter, Guid? companyId, string? search = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<byte[]>> ExportTenantsAsync(ExportFormat format, string? search = null, CancellationToken cancellationToken = default);
    
    // Subdomain Management (Super Admin)
    Task<ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>> GetSubdomainsPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ReassignSubdomainAsync(Guid profileId, ReassignSubdomainDto dto);
}
