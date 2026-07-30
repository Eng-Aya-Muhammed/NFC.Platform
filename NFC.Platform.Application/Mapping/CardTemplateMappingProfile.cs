using System;
using System.Globalization;
using AutoMapper;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping;

public class CardTemplateMappingProfile : Profile
{
    public CardTemplateMappingProfile()
    {
        CreateMap<CardTemplate, CardTemplateDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src =>
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase)
                    ? (string.IsNullOrWhiteSpace(src.NameAr) ? src.NameEn : src.NameAr)
                    : (string.IsNullOrWhiteSpace(src.NameEn) ? src.NameAr : src.NameEn)))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src =>
                src.Category != null
                    ? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase)
                        ? (string.IsNullOrWhiteSpace(src.Category.NameAr) ? src.Category.NameEn : src.Category.NameAr)
                        : (string.IsNullOrWhiteSpace(src.Category.NameEn) ? src.Category.NameAr : src.Category.NameEn))
                    : null));

        CreateMap<CardTemplate, CardTemplateAdminDto>()
            .ForMember(dest => dest.CategoryNameAr, opt => opt.MapFrom(src => src.Category != null ? src.Category.NameAr : string.Empty))
            .ForMember(dest => dest.CategoryNameEn, opt => opt.MapFrom(src => src.Category != null ? src.Category.NameEn : string.Empty));
        CreateMap<CardTemplate, CardTemplateExportDto>();

        CreateMap<CreateCardTemplateRequest, CardTemplate>();

        CreateMap<UpdateCardTemplateRequest, CardTemplate>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
