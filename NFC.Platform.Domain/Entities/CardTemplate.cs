using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class CardTemplate : BaseEntity, ITenantEntity
    {
        public Guid? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        Guid ITenantEntity.TenantId
        {
            get => TenantId ?? Guid.Empty;
            set => TenantId = value == Guid.Empty ? null : value;
        }

        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string StyleConfigJson { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
