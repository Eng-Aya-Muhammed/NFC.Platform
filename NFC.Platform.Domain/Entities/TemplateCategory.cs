using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class TemplateCategory : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}
