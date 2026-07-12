using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Domain.Entities;
using System;

namespace NFC.Platform.Application.Mapping
{
    public class SubscriptionMappingProfile : Profile
    {
        public SubscriptionMappingProfile()
        {
            CreateMap<SubscriptionPlan, SubscriptionPlanDto>();

            CreateMap<UserSubscription, UserSubscriptionDto>()
                .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.SubscriptionPlan != null ? src.SubscriptionPlan.Name : string.Empty))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.SubscriptionPlan != null ? src.SubscriptionPlan.Price : 0m))
                .ForMember(dest => dest.RemainingDays, opt => opt.MapFrom(src => (src.EndDate - DateTime.UtcNow).Days > 0 ? (src.EndDate - DateTime.UtcNow).Days : 0));

            CreateMap<RenewSubscriptionRequest, UserSubscription>();
            CreateMap<SubscribeRequest, UserSubscription>();
        }
    }
}
