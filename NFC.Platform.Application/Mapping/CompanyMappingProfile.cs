using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Auth;
using NFC.Platform.Application.DTOs.Company;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping
{
    public class CompanyMappingProfile : Profile
    {
        public CompanyMappingProfile()
        {
            CreateMap<Company, CompanyProfileDto>()
                .ForMember(dest => dest.AdminUserEmail, opt => opt.MapFrom(src => src.AdminUser != null ? src.AdminUser.Email : string.Empty))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.AdminUser != null ? src.AdminUser.PhoneNumber : string.Empty))
                .ForMember(dest => dest.SubscriptionRemainingDays, opt => opt.Ignore())
                .ForMember(dest => dest.Links, opt => opt.MapFrom(src => src.AdminUser != null && src.AdminUser.UserProfile != null ? src.AdminUser.UserProfile.CustomLinks.OrderBy(l => l.DisplayOrder) : System.Linq.Enumerable.Empty<ProfileLink>()));

            CreateMap<UpdateCompanyProfileRequest, Company>()
                .ForMember(dest => dest.Name, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Name)))
                .ForMember(dest => dest.Address, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Address)))
                .ForMember(dest => dest.Activity, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Activity)))
                .ForMember(dest => dest.CommercialRegistry, opt => opt.Condition(src => !string.IsNullOrEmpty(src.CommercialRegistry)))
                .ForMember(dest => dest.CompanySize, opt => opt.Condition(src => src.CompanySize.HasValue));

            CreateMap<RegisterRequest, Company>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.CompanyName ?? string.Empty))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address ?? string.Empty))
                .ForMember(dest => dest.Activity, opt => opt.MapFrom(src => src.IndustryType ?? string.Empty))
                .ForMember(dest => dest.CompanySize, opt => opt.MapFrom(src => src.CompanySize))
                .ForMember(dest => dest.CommercialRegistry, opt => opt.MapFrom(src => src.CommercialRegistrationNumber ?? string.Empty));
        }
    }
}
