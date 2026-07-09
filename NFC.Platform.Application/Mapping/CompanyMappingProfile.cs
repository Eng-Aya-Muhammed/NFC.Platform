using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping
{
    public class CompanyMappingProfile : Profile
    {
        public CompanyMappingProfile()
        {
            CreateMap<Company, CompanyProfileDto>()
                .ForMember(dest => dest.AdminUserEmail, opt => opt.MapFrom(src => src.AdminUser != null ? src.AdminUser.Email : string.Empty))
                .ForMember(dest => dest.SubscriptionRemainingDays, opt => opt.Ignore());

            CreateMap<UpdateCompanyProfileRequest, Company>();
        }
    }
}
