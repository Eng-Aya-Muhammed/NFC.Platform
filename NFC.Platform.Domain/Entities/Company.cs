using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Domain.Entities
{
    public class Company : BaseEntity, ITenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string CommercialRegistry { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public CompanySize? CompanySize { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Bio { get; set; }
        public string? WebsiteUrl { get; set; }

        public string? IndustryType { get => Activity; set => Activity = value ?? string.Empty; }
        public string? CommercialRegistrationNumber { get => CommercialRegistry; set => CommercialRegistry = value ?? string.Empty; }
        public bool IsVip { get; set; } = false;
        public int VipDisplayOrder { get; set; } = 0;

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid AdminUserId { get; set; }
        public User AdminUser { get; set; } = null!;


        public Guid? ProfileTemplateId { get; set; }
        public CardTemplate? ProfileTemplate { get; set; }

        public ICollection<Employee> Employees { get; set; } = [];
    }
}
