using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping
{
    public class CardTemplateMappingProfile : Profile
    {
        public CardTemplateMappingProfile()
        {
            CreateMap<CardTemplate, CardTemplateDto>()
                .ForMember(dest => dest.PreviewImageUrl, opt => opt.MapFrom(src => src.ThumbnailUrl));
        }
    }
}
