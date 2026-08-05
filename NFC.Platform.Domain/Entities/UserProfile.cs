using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Enums;

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

        public PreferredLanguage PreferredLanguage { get; set; } = PreferredLanguage.Arabic;

        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }
        public string? Address { get; set; }
        public bool IsVip { get; set; } = false;
        public int VipDisplayOrder { get; set; } = 0;


        public Guid? ProfileTemplateId { get; set; }
        public CardTemplate? ProfileTemplate { get; set; }

        public ICollection<ProfileLink> CustomLinks { get; set; } = [];

        public string? Subdomain { get; set; }


    }
}
