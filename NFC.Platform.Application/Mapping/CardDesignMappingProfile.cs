using System;
using System.Globalization;
using AutoMapper;
using NFC.Platform.Application.DTOs.CardDesign;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping;

public class CardDesignMappingProfile : Profile
{
    public CardDesignMappingProfile()
    {
        CreateMap<CardDesign, CardDesignDto>()
            .ForMember(dest => dest.CardTypeName,
                       opt => opt.MapFrom(src => src.CardType != null
                           ? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase)
                               ? (string.IsNullOrWhiteSpace(src.CardType.NameAr) ? src.CardType.NameEn : src.CardType.NameAr)
                               : (string.IsNullOrWhiteSpace(src.CardType.NameEn) ? src.CardType.NameAr : src.CardType.NameEn))
                           : null))
            .ForMember(dest => dest.CardPackageName,
                       opt => opt.MapFrom(src => src.CardPackage != null ? $"{src.CardPackage.NumberOfCards} Cards Package" : null))
            .ForMember(dest => dest.RemainingQuantity,
                       opt => opt.MapFrom(src => src.TotalQuantity - src.UsedQuantity));

        CreateMap<CreateCardDesignRequest, CardDesign>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CardPackageId, opt => opt.Ignore())
            .ForMember(dest => dest.CardPackage, opt => opt.Ignore())
            .ForMember(dest => dest.TotalQuantity, opt => opt.Ignore())
            .ForMember(dest => dest.UsedQuantity, opt => opt.Ignore())
            .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.IsPaid, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentStatus, opt => opt.Ignore())
            .ForMember(dest => dest.PaidAt, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentTransactionId, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Orders, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
    }
}
