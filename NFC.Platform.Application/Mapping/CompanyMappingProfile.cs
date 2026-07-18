using AutoMapper;
using NFC.Platform.Application.DTOs;
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
                .ForMember(dest => dest.SubscriptionRemainingDays, opt => opt.Ignore());
            // ProfileTemplateId — mapped by convention from Company entity

            CreateMap<UpdateCompanyProfileRequest, Company>();

            // Template update — only map non-null overrides; null means "keep existing"
            CreateMap<UpdateCompanyTemplateRequest, Company>()
                .ForMember(dest => dest.ProfileTemplateId, opt => opt.Condition(src => src.ProfileTemplateId != null))
                // Navigation and non-template fields are ignored
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) =>
                    srcMember != null));
        }
    }
}
