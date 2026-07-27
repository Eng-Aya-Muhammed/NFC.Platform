using AutoMapper;
using NFC.Platform.Application.DTOs.VipCustomer;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Mapping;

public class VipCustomerMappingProfile : Profile
{
    public VipCustomerMappingProfile()
    {
        CreateMap<Company, VipCustomerDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.LogoUrl ?? string.Empty))
            .ForMember(dest => dest.CustomerType, opt => opt.MapFrom(_ => VipCustomerType.Company));

        CreateMap<UserProfile, VipCustomerDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ProfilePictureUrl ?? string.Empty))
            .ForMember(dest => dest.CustomerType, opt => opt.MapFrom(_ => VipCustomerType.Individual));
    }
}
