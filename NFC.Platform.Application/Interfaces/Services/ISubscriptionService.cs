using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface ISubscriptionService
    {
        Task<ServiceResult<IReadOnlyList<SubscriptionPlanDto>>> GetPlansAsync();
        Task<ServiceResult<UserSubscriptionDto>> GetCurrentSubscriptionAsync();
        Task<ServiceResult<IReadOnlyList<UserSubscriptionDto>>> GetSubscriptionHistoryAsync();
        Task<ServiceResult<UserSubscriptionDto>> SubscribeAsync(SubscribeRequest request);
        Task<ServiceResult<UserSubscriptionDto>> RenewSubscriptionAsync(RenewSubscriptionRequest request);
    }
}
