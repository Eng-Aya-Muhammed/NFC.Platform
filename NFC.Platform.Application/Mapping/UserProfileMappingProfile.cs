using System;
using System.Linq;
using AutoMapper;
using NFC.Platform.Application.Constants;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Mapping;

public class UserProfileMappingProfile : Profile
{
    public UserProfileMappingProfile()
    {
        CreateMap<ProfileLink, ProfileLinkDto>();

        CreateMap<UserProfile, EmployeeDetailsDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.EmployeeId ?? src.UserId ?? src.Id))
            .ForMember(dest => dest.ProfileId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Subdomain, opt => opt.MapFrom(src => src.Subdomain))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Status.ToString() : (src.User != null ? src.User.Status.ToString() : UserStatus.Active.ToString())))
            .ForMember(dest => dest.Links, opt => opt.MapFrom(src => src.CustomLinks.OrderBy(l => l.DisplayOrder)))
            // Branding fields are resolved manually in ProfileMetricService.ApplyBranding — not mapped from entity
            .ForMember(dest => dest.LogoUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Layout, opt => opt.Ignore())
            .ForMember(dest => dest.StyleConfigJson, opt => opt.Ignore());

        CreateMap<User, EmployeeDetailsDto>()
            .ConvertUsing((src, dest, ctx) =>
            {
                var dto = ctx.Mapper.Map<EmployeeDetailsDto>(src.UserProfile);
                if (dto != null)
                {
                    dto.Id = src.Id;
                    dto.Email = src.Email;
                    dto.Status = src.Status.ToString();
                }
                return dto!;
            });

        CreateMap<CreateEmployeeRequest, UserProfile>()
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle ?? string.Empty))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department ?? string.Empty))
            .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.CustomLinks, opt => opt.Ignore());

        CreateMap<UpdateMyProfileRequest, UserProfile>()
            // Subdomain is handled manually in ProfileService.UpdateProfileAsync (slug + uniqueness check)
            .ForMember(dest => dest.Subdomain, opt => opt.Ignore());

        CreateMap<UpdateEmployeeRequest, UserProfile>()
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle ?? string.Empty))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department ?? string.Empty))
            .ForMember(dest => dest.FullName, opt => {
                opt.Condition(src => src.FullName != null);
                opt.MapFrom(src => src.FullName);
            })
            .ForMember(dest => dest.CustomLinks, opt => opt.Ignore());

        CreateMap<User, UserProfile>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CustomLinks, opt => opt.Ignore());

        CreateMap<RecordMetricRequest, ProfileMetric>()
            .ForMember(dest => dest.UserProfileId, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore());
    }
}
