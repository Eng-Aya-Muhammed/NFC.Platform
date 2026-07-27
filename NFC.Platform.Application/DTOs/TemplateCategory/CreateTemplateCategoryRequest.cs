namespace NFC.Platform.Application.DTOs.TemplateCategory;

public class CreateTemplateCategoryRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}
