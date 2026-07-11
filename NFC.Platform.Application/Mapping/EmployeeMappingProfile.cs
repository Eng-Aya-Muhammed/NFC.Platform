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

        CreateMap<UserProfile, EmployeeDetailsDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Status.ToString() : (src.User != null ? src.User.Status.ToString() : "Active")))
            .ForMember(dest => dest.CustomLinks, opt => opt.MapFrom(src => src.CustomLinks.OrderBy(l => l.DisplayOrder)));

        CreateMap<UpdateMyProfileRequest, UserProfile>();
    }
}
