using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Mapping
{
    public class TemplateRequestMappingProfile : Profile
    {
        public TemplateRequestMappingProfile()
        {
            CreateMap<TemplateRequest, TemplateRequestDto>()
                .ForMember(dest => dest.RequestedByUsername, opt => opt.MapFrom(src => src.RequestedByUser != null ? src.RequestedByUser.Username : string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.RequestType, opt => opt.MapFrom(src => src.RequestType.ToString()));

            CreateMap<CreateTemplateRequest, TemplateRequest>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => TemplateRequestStatus.Pending));
        }
    }
}
