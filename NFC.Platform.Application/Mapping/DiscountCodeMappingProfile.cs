using AutoMapper;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping;

public class DiscountCodeMappingProfile : Profile
{
    public DiscountCodeMappingProfile()
    {
        CreateMap<DiscountCode, DiscountCodeDto>();

        CreateMap<CreateDiscountCodeRequest, DiscountCode>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpperInvariant()));

        CreateMap<UpdateDiscountCodeRequest, DiscountCode>()
            .ForMember(dest => dest.Code, opt => {
                opt.Condition(src => !string.IsNullOrWhiteSpace(src.Code));
                opt.MapFrom(src => src.Code!.Trim().ToUpperInvariant());
            })
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
