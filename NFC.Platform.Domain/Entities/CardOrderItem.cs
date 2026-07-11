using System;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class CardOrderItem : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid CardOrderId { get; set; }
        public CardOrder CardOrder { get; set; } = null!;

        public string EmployeeName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Department { get; set; }

        public string? ActivationCode { get; set; }

        public Guid? LinkedCardId { get; set; }
        public Card? LinkedCard { get; set; }

        public Guid? UserProfileId { get; set; }
        public UserProfile? UserProfile { get; set; }
    }
}
