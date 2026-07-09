using AutoMapper;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping
{
    public class CardOrderMappingProfile : Profile
    {
        public CardOrderMappingProfile()
        {
            CreateMap<CardOrder, CardOrderDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<CardOrderItem, CardOrderItemDto>();

            CreateMap<CreateCardOrderRequest, CardOrder>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<CreateCardOrderItemRequest, CardOrderItem>()
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CardOrderId, opt => opt.Ignore())
                .ForMember(dest => dest.ActivationCode, opt => opt.Ignore())
                .ForMember(dest => dest.LinkedCardId, opt => opt.Ignore());
        }
    }
}
