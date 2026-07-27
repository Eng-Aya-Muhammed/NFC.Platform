using System;
using System.Globalization;
using System.Linq;
using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping
{
    public class SubscriptionMappingProfile : Profile
    {
        public SubscriptionMappingProfile()
        {
            //  SubscriptionPlan -> SubscriptionPlanDto (User localized)
            CreateMap<SubscriptionPlan, SubscriptionPlanDto>()
                .ForMember(d => d.Name,
                    opt => opt.MapFrom(src =>
                        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar"
                            ? (string.IsNullOrWhiteSpace(src.NameAr) ? src.NameEn : src.NameAr)
                            : (string.IsNullOrWhiteSpace(src.NameEn) ? src.NameAr : src.NameEn)))
                .ForMember(d => d.AllowedTemplates,
                    opt => opt.MapFrom(src =>
                        src.PlanTemplates.Select(pt => pt.CardTemplate)));

            //  SubscriptionPlan -> SubscriptionPlanAdminDto (Admin NameAr + NameEn)
            CreateMap<SubscriptionPlan, SubscriptionPlanAdminDto>()
                .ForMember(d => d.AllowedTemplates,
                    opt => opt.MapFrom(src =>
                        src.PlanTemplates.Select(pt => pt.CardTemplate)));

            //  CardTemplate -> CardTemplateSummaryDto (embedded in plan)
            CreateMap<CardTemplate, CardTemplateSummaryDto>();

            //  UserSubscription -> UserSubscriptionDto
            CreateMap<UserSubscription, UserSubscriptionDto>()
                .ForMember(d => d.PlanName,
                    opt => opt.MapFrom(src =>
                        src.SubscriptionPlan != null
                            ? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar"
                                ? (string.IsNullOrWhiteSpace(src.SubscriptionPlan.NameAr) ? src.SubscriptionPlan.NameEn : src.SubscriptionPlan.NameAr)
                                : (string.IsNullOrWhiteSpace(src.SubscriptionPlan.NameEn) ? src.SubscriptionPlan.NameAr : src.SubscriptionPlan.NameEn))
                            : string.Empty))
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

            //  Request -> Entity
            CreateMap<RenewSubscriptionRequest, UserSubscription>();
            CreateMap<SubscribeRequest, UserSubscription>();
            CreateMap<CreateSubscriptionPlanRequest, SubscriptionPlan>()
                .ForMember(d => d.PlanTemplates, opt => opt.Ignore());
            CreateMap<UpdateSubscriptionPlanRequest, SubscriptionPlan>()
                .ForMember(d => d.PlanTemplates, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
