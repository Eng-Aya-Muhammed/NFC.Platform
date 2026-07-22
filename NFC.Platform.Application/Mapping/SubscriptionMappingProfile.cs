using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.Domain.Entities;
using System;

namespace NFC.Platform.Application.Mapping
{
    public class SubscriptionMappingProfile : Profile
    {
        public SubscriptionMappingProfile()
        {
            // ── SubscriptionPlan → SubscriptionPlanDto ────────────────────────
            CreateMap<SubscriptionPlan, SubscriptionPlanDto>()
                .ForMember(d => d.AllowedTemplates,
                    opt => opt.MapFrom(src =>
                        src.PlanTemplates.Select(pt => pt.CardTemplate)));

            // ── CardTemplate → CardTemplateSummaryDto (embedded in plan) ──────
            CreateMap<CardTemplate, CardTemplateSummaryDto>();

            // ── UserSubscription → UserSubscriptionDto ────────────────────────
            CreateMap<UserSubscription, UserSubscriptionDto>()
                .ForMember(d => d.PlanName,
                    opt => opt.MapFrom(src =>
                        src.SubscriptionPlan != null ? src.SubscriptionPlan.Name : string.Empty))
                .ForMember(d => d.Price,
                    opt => opt.MapFrom(src =>
                        src.SubscriptionPlan != null ? src.SubscriptionPlan.Price : 0m))
                .ForMember(d => d.MaxTemplateChanges,
                    opt => opt.MapFrom(src =>
                        src.SubscriptionPlan != null ? src.SubscriptionPlan.MaxTemplateChanges : 0))
                .ForMember(d => d.MaxCustomDesignRequests,
                    opt => opt.MapFrom(src =>
                        src.SubscriptionPlan != null ? src.SubscriptionPlan.MaxCustomDesignRequests : 0))
                .ForMember(d => d.RemainingDays,
                    opt => opt.MapFrom(src =>
                        (src.EndDate - DateTime.UtcNow).Days > 0
                            ? (src.EndDate - DateTime.UtcNow).Days
                            : 0));

            // ── Request → Entity ──────────────────────────────────────────────
            CreateMap<RenewSubscriptionRequest, UserSubscription>();
            CreateMap<SubscribeRequest, UserSubscription>();
            CreateMap<CreateSubscriptionPlanRequest, SubscriptionPlan>()
                .ForMember(d => d.PlanTemplates, opt => opt.Ignore());
        }
    }
}
