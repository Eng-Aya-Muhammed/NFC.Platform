
namespace NFC.Platform.Application.Mapping;

public class CardPackageMappingProfile : Profile
{
    public CardPackageMappingProfile()
    {
        CreateMap<CardPackage, CardPackageDto>();
        CreateMap<CardPackage, CardPackageAdminDto>();
        CreateMap<CardPackage, CardPackageExportDto>();

        CreateMap<CreateCardPackageRequest, CardPackage>();

        CreateMap<UpdateCardPackageRequest, CardPackage>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
