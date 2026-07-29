using AutoMapper;
using NFC.Platform.Application.DTOs.CardDesign;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping;

public class CardDesignMappingProfile : Profile
{
    public CardDesignMappingProfile()
    {
        // ── CardDesign → CardDesignDto ─────────────────────────────────────────
        CreateMap<CardDesign, CardDesignDto>()
            .ForMember(dest => dest.CardTypeName,
                       opt => opt.MapFrom(src => src.CardType != null ? src.CardType.NameAr : null))
            .ForMember(dest => dest.RemainingQuantity,
                       opt => opt.MapFrom(src => src.TotalQuantity - src.UsedQuantity));

        // ── CreateCardDesignRequest → CardDesign ──────────────────────────────
        // Pricing, TenantId, UserId, and payment fields are set manually in the Service.
        CreateMap<CreateCardDesignRequest, CardDesign>()
            .ForMember(dest => dest.Id,                   opt => opt.Ignore())
            .ForMember(dest => dest.TenantId,             opt => opt.Ignore())
            .ForMember(dest => dest.Tenant,               opt => opt.Ignore())
            .ForMember(dest => dest.UserId,               opt => opt.Ignore())
            .ForMember(dest => dest.User,                 opt => opt.Ignore())
            .ForMember(dest => dest.CardPackageId,        opt => opt.Ignore()) // set in Service
            .ForMember(dest => dest.CardPackage,          opt => opt.Ignore())
            .ForMember(dest => dest.TotalQuantity,        opt => opt.Ignore()) // computed in Service
            .ForMember(dest => dest.UsedQuantity,         opt => opt.Ignore())
            .ForMember(dest => dest.UnitPrice,            opt => opt.Ignore()) // computed in Service
            .ForMember(dest => dest.TotalPrice,           opt => opt.Ignore()) // computed in Service
            .ForMember(dest => dest.Currency,             opt => opt.Ignore())
            .ForMember(dest => dest.IsPaid,               opt => opt.Ignore())
            .ForMember(dest => dest.PaymentStatus,        opt => opt.Ignore())
            .ForMember(dest => dest.PaidAt,               opt => opt.Ignore())
            .ForMember(dest => dest.PaymentTransactionId, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion,           opt => opt.Ignore())
            .ForMember(dest => dest.Orders,               opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,            opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,            opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy,            opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy,            opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,            opt => opt.Ignore());
    }
}
