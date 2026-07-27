using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class CardType : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
