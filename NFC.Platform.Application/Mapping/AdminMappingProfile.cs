using AutoMapper;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping
{
    public class AdminMappingProfile : Profile
    {
        public AdminMappingProfile()
        {
            CreateMap<CardOrder, AdminOrderSummaryDto>()
                .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                .ForMember(dest => dest.Material, opt => opt.MapFrom(src => src.CardType))
                .ForMember(dest => dest.DesignType, opt => opt.MapFrom(src => src.CardDesignType));

            CreateMap<CardOrder, AdminOrderDetailDto>()
                .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? (src.User.UserProfile != null ? src.User.UserProfile.FullName : src.User.Username) : string.Empty))
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
                .ForMember(dest => dest.Material, opt => opt.MapFrom(src => src.CardType))
                .ForMember(dest => dest.DesignType, opt => opt.MapFrom(src => src.CardDesignType))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<CreateCardTemplateDto, CardTemplate>();
            CreateMap<UpdateCardTemplateDto, CardTemplate>();
            CreateMap<Tenant, TenantSummaryDto>();
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            CreateMap<UserProfile, ProfileSubdomainSummaryDto>()
                .ForMember(dest => dest.ProfileId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src =>
                    src.Employee != null && src.Employee.Company != null
                        ? src.Employee.Company.Name
                        : src.CompanyName));
        }
    }
}
