

namespace NFC.Platform.Application.Mapping;

public class CardTypeMappingProfile : Profile
{
    public CardTypeMappingProfile()
    {
        CreateMap<CardType, CardTypeDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src =>
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase)
                    ? (string.IsNullOrWhiteSpace(src.NameAr) ? src.NameEn : src.NameAr)
                    : (string.IsNullOrWhiteSpace(src.NameEn) ? src.NameAr : src.NameEn)));

        CreateMap<CardType, CardTypeAdminDto>();

        CreateMap<CreateCardTypeRequest, CardType>();

        CreateMap<UpdateCardTypeRequest, CardType>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
