using AutoMapper;
using NFC.Platform.Application.DTOs.Auth;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Mapping
{
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            CreateMap<GoogleRegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            CreateMap<User, AuthDto>()
                .ForMember(dest => dest.Token, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
        }
    }
}
