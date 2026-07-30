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
                .ForMember(dest => dest.CardName, opt => opt.MapFrom(src => src.CardDesign != null && src.CardDesign.CardType != null
                    ? (!string.IsNullOrWhiteSpace(src.CardDesign.CardType.NameAr) && !string.IsNullOrWhiteSpace(src.CardDesign.CardType.NameEn)
                        ? $"{src.CardDesign.CardType.NameAr} / {src.CardDesign.CardType.NameEn}"
                        : src.CardDesign.CardType.NameAr ?? src.CardDesign.CardType.NameEn ?? string.Empty)
                    : string.Empty))
                .ForMember(dest => dest.CardTypeId, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.CardTypeId : System.Guid.Empty))
                .ForMember(dest => dest.CardPackageId, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.CardPackageId : System.Guid.Empty))
                .ForMember(dest => dest.DesignType, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.CardDesignType : default));

            CreateMap<CardOrder, AdminOrderDetailDto>()
                .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? (src.User.UserProfile != null ? src.User.UserProfile.FullName : src.User.Username) : string.Empty))
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
                .ForMember(dest => dest.CardName, opt => opt.MapFrom(src => src.CardDesign != null && src.CardDesign.CardType != null
                    ? (!string.IsNullOrWhiteSpace(src.CardDesign.CardType.NameAr) && !string.IsNullOrWhiteSpace(src.CardDesign.CardType.NameEn)
                        ? $"{src.CardDesign.CardType.NameAr} / {src.CardDesign.CardType.NameEn}"
                        : src.CardDesign.CardType.NameAr ?? src.CardDesign.CardType.NameEn ?? string.Empty)
                    : string.Empty))
                .ForMember(dest => dest.CardTypeId, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.CardTypeId : System.Guid.Empty))
                .ForMember(dest => dest.CardPackageId, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.CardPackageId : System.Guid.Empty))
                .ForMember(dest => dest.ExcelDataUrl, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.ExcelDataUrl : null))
                .ForMember(dest => dest.FrontDesignUrl, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.FrontDesignUrl : null))
                .ForMember(dest => dest.BackDesignUrl, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.BackDesignUrl : null))
                .ForMember(dest => dest.DesignType, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.CardDesignType : default))
                .ForMember(dest => dest.CardType, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.CardType : null))
                .ForMember(dest => dest.CardPackage, opt => opt.MapFrom(src => src.CardDesign != null ? src.CardDesign.CardPackage : null))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

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
