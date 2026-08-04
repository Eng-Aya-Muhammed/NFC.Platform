using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Mapping;

public class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Subdomain, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.Subdomain : null));

        CreateMap<Employee, EmployeeExportDto>()
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.Phone : string.Empty))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == UserStatus.Active));

        CreateMap<Employee, EmployeeDetailsDto>()
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

        CreateMap<CreateEmployeeRequest, Employee>()
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle ?? string.Empty))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department ?? string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => UserStatus.Active));

        CreateMap<UpdateEmployeeRequest, Employee>()
            .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle ?? string.Empty))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department ?? string.Empty))
            .ForMember(dest => dest.FullName, opt => {
                opt.Condition(src => src.FullName != null);
                opt.MapFrom(src => src.FullName);
            });
    }
}
