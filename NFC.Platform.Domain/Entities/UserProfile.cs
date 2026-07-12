using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class UserProfile : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public Guid? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }

        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }

        public ICollection<ProfileLink> CustomLinks { get; set; } = [];

        public ICollection<Card> ActivatedCards { get; set; } = [];
    }
}
