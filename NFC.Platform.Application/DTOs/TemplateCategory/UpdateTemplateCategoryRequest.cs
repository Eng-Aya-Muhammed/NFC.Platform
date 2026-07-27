namespace NFC.Platform.Application.DTOs.TemplateCategory;

public class UpdateTemplateCategoryRequest
{
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}
