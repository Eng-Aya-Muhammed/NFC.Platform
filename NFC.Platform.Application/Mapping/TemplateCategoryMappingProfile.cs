
namespace NFC.Platform.Application.Mapping;

public class TemplateCategoryMappingProfile : Profile
{
    public TemplateCategoryMappingProfile()
    {
        CreateMap<TemplateCategory, TemplateCategoryDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src =>
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase)
                    ? (string.IsNullOrWhiteSpace(src.NameAr) ? src.NameEn : src.NameAr)
                    : (string.IsNullOrWhiteSpace(src.NameEn) ? src.NameAr : src.NameEn)));

        CreateMap<TemplateCategory, TemplateCategoryAdminDto>();
        CreateMap<TemplateCategory, TemplateCategoryExportDto>();

        CreateMap<CreateTemplateCategoryRequest, TemplateCategory>();

        CreateMap<UpdateTemplateCategoryRequest, TemplateCategory>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
