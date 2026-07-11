using System.Linq;
using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping;

public class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        CreateMap<ProfileLink, ProfileLinkDto>();

        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Employee, EmployeeDetailsDto>()
            .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.Bio : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.ContactEmail : string.Empty))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.Phone : string.Empty))
            .ForMember(dest => dest.WhatsApp, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.WhatsApp : string.Empty))
            .ForMember(dest => dest.InstagramUrl, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.InstagramUrl : string.Empty))
            .ForMember(dest => dest.FacebookUrl, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.FacebookUrl : string.Empty))
            .ForMember(dest => dest.LinkedInUrl, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.LinkedInUrl : string.Empty))
            .ForMember(dest => dest.WebsiteUrl, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.WebsiteUrl : string.Empty));

        CreateMap<User, EmployeeDetailsDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.FullName : string.Empty))
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.JobTitle : string.Empty))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.Department : string.Empty))
            .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.Bio : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.ContactEmail : string.Empty))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.Phone : string.Empty))
            .ForMember(dest => dest.WhatsApp, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.WhatsApp : string.Empty))
            .ForMember(dest => dest.InstagramUrl, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.InstagramUrl : string.Empty))
            .ForMember(dest => dest.FacebookUrl, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.FacebookUrl : string.Empty))
            .ForMember(dest => dest.LinkedInUrl, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.LinkedInUrl : string.Empty))
            .ForMember(dest => dest.WebsiteUrl, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.WebsiteUrl : string.Empty));

        CreateMap<UserProfile, EmployeeDetailsDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Status.ToString() : (src.User != null ? src.User.Status.ToString() : "Active")))
            .ForMember(dest => dest.CustomLinks, opt => opt.MapFrom(src => src.CustomLinks.OrderBy(l => l.DisplayOrder)));

        CreateMap<UpdateMyProfileRequest, UserProfile>();
    }
}
