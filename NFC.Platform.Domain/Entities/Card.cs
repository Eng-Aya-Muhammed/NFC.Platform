using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class Card : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public string ActivationCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = false;

        public DateTime? ActivatedAt { get; set; }

        public Guid? UserProfileId { get; set; }
        public UserProfile? UserProfile { get; set; }

        public Guid? CardOrderId { get; set; }
        public CardOrder? CardOrder { get; set; }
    }
}
