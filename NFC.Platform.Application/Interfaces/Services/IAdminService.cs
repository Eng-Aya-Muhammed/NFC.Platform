using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IAdminService
    {
        Task<ServiceResult<PagedResult<AdminOrderSummaryDto>>> GetOrdersPagedAsync(PaginationRequest request, OrderStatus? statusFilter, Guid? companyId = null, CancellationToken cancellationToken = default);
        Task<ServiceResult<AdminOrderDetailDto>> GetOrderByIdAsync(Guid id);
        Task<ServiceResult> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto);
        Task<ServiceResult> VerifyDeliveryOtpAsync(Guid orderId, string otp);
        Task<ServiceResult> ResendDeliveryOtpAsync(Guid orderId);
        Task<ServiceResult<PagedResult<TemplateRequestDto>>> GetTemplateRequestsPagedAsync(PaginationRequest request, TemplateRequestStatus? status = null, CancellationToken cancellationToken = default);
        Task<ServiceResult> ResolveTemplateRequestAsync(Guid id, ResolveTemplateRequestDto dto);
        Task<ServiceResult<CardTemplateDto>> CreateTemplateAsync(CreateCardTemplateDto dto);
        Task<ServiceResult<CardTemplateDto>> UpdateTemplateAsync(Guid id, UpdateCardTemplateDto dto);
        Task<ServiceResult> DeleteTemplateAsync(Guid id);
        Task<ServiceResult<PagedResult<TenantSummaryDto>>> GetTenantsPagedAsync(PaginationRequest request, CancellationToken cancellationToken = default);
        Task<ServiceResult> UpdateTenantStatusAsync(Guid id, UpdateTenantStatusDto dto);
        Task<ServiceResult> UpdateCardPricingAsync(UpdateCardPricingDto dto);

        //  Subdomain management (Super Admin) 
        Task<ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>> GetAllProfileSubdomainsAsync(PaginationRequest request, string? search, CancellationToken cancellationToken = default);
        Task<ServiceResult> ReassignSubdomainAsync(Guid profileId, string newSubdomain);

        //  Subscription Plan Management (Super Admin) 
        Task<ServiceResult<SubscriptionPlanDto>> CreatePlanAsync(CreateSubscriptionPlanRequest request);
        Task<ServiceResult<SubscriptionPlanDto>> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanRequest request);
        Task<ServiceResult> DeletePlanAsync(Guid planId);

        //  Plan Template Assignment (Super Admin) 
        Task<ServiceResult<IReadOnlyList<CardTemplateSummaryDto>>> GetPlanTemplatesAsync(Guid planId);
        Task<ServiceResult> AssignTemplateAsync(Guid planId, Guid templateId);
        Task<ServiceResult> UnassignTemplateAsync(Guid planId, Guid templateId);
    }
}


