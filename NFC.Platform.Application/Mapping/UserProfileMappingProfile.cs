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
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Status.ToString() : (src.User != null ? src.User.Status.ToString() : UserStatus.Active.ToString())))
            .ForMember(dest => dest.LinkedInUrl, opt => opt.MapFrom(src => src.CustomLinks.FirstOrDefault(l => l.Title == PlatformConstants.LinkedIn) != null ? src.CustomLinks.FirstOrDefault(l => l.Title == PlatformConstants.LinkedIn)!.Url : string.Empty))
            .ForMember(dest => dest.FacebookUrl, opt => opt.MapFrom(src => src.CustomLinks.FirstOrDefault(l => l.Title == PlatformConstants.Facebook) != null ? src.CustomLinks.FirstOrDefault(l => l.Title == PlatformConstants.Facebook)!.Url : string.Empty))
            .ForMember(dest => dest.InstagramUrl, opt => opt.MapFrom(src => src.CustomLinks.FirstOrDefault(l => l.Title == PlatformConstants.Instagram) != null ? src.CustomLinks.FirstOrDefault(l => l.Title == PlatformConstants.Instagram)!.Url : string.Empty))
            .ForMember(dest => dest.WebsiteUrl, opt => opt.MapFrom(src => src.CustomLinks.FirstOrDefault(l => l.Title == PlatformConstants.Website) != null ? src.CustomLinks.FirstOrDefault(l => l.Title == PlatformConstants.Website)!.Url : string.Empty))
            .ForMember(dest => dest.CustomLinks, opt => opt.MapFrom(src => src.CustomLinks
                .Where(l => l.Title != PlatformConstants.LinkedIn && l.Title != PlatformConstants.Facebook && l.Title != PlatformConstants.Instagram && l.Title != PlatformConstants.Website)
                .OrderBy(l => l.DisplayOrder)));

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
            .ForMember(dest => dest.CustomLinks, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (!string.IsNullOrEmpty(src.LinkedInUrl))
                    dest.CustomLinks.Add(new ProfileLink { Id = Guid.Empty, Title = PlatformConstants.LinkedIn, Url = src.LinkedInUrl });
                if (!string.IsNullOrEmpty(src.FacebookUrl))
                    dest.CustomLinks.Add(new ProfileLink { Id = Guid.Empty, Title = PlatformConstants.Facebook, Url = src.FacebookUrl });
                if (!string.IsNullOrEmpty(src.InstagramUrl))
                    dest.CustomLinks.Add(new ProfileLink { Id = Guid.Empty, Title = PlatformConstants.Instagram, Url = src.InstagramUrl });
                if (!string.IsNullOrEmpty(src.WebsiteUrl))
                    dest.CustomLinks.Add(new ProfileLink { Id = Guid.Empty, Title = PlatformConstants.Website, Url = src.WebsiteUrl });
            });

        CreateMap<UpdateMyProfileRequest, UserProfile>()
            .AfterMap((src, dest) =>
            {
                UpdateStandardLink(dest, PlatformConstants.LinkedIn, src.LinkedInUrl);
                UpdateStandardLink(dest, PlatformConstants.Facebook, src.FacebookUrl);
                UpdateStandardLink(dest, PlatformConstants.Instagram, src.InstagramUrl);
                UpdateStandardLink(dest, PlatformConstants.Website, src.WebsiteUrl);
            });

        CreateMap<UpdateEmployeeRequest, UserProfile>()
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle ?? string.Empty))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department ?? string.Empty))
            .ForMember(dest => dest.FullName, opt => {
                opt.Condition(src => src.FullName != null);
                opt.MapFrom(src => src.FullName);
            })
            .ForMember(dest => dest.CustomLinks, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                UpdateStandardLink(dest, PlatformConstants.LinkedIn, src.LinkedInUrl);
                UpdateStandardLink(dest, PlatformConstants.Facebook, src.FacebookUrl);
                UpdateStandardLink(dest, PlatformConstants.Instagram, src.InstagramUrl);
                UpdateStandardLink(dest, PlatformConstants.Website, src.WebsiteUrl);
            });
    }

    private static void UpdateStandardLink(UserProfile dest, string title, string? url)
    {
        var existing = dest.CustomLinks.FirstOrDefault(l => l.Title == title);
        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                dest.CustomLinks.Remove(existing);
            }
            else
            {
                existing.Url = url;
            }
        }
        else if (!string.IsNullOrWhiteSpace(url))
        {
            dest.CustomLinks.Add(new ProfileLink
            {
                Id = Guid.Empty,
                Title = title,
                Url = url,
                TenantId = dest.TenantId,
                UserProfileId = dest.Id
            });
        }
    }
}
