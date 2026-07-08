using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class ProfileLink : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
