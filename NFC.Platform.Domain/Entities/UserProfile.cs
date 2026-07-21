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

        /// <summary>
        /// URL-safe subdomain slug for the public digital profile.
        /// e.g. "ghaith" → ghaith.on-point-kw.com
        /// Globally unique across all tenants.
        /// </summary>
        public string? Subdomain { get; set; }

        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }

        // ── Digital profile branding (individual accounts only) ───────────────

        /// <summary>
        /// FK to the CardTemplate defining the digital profile layout for this individual account.
        /// Used only when this profile is not linked to a Company. Set via PATCH /api/user/profile/template.
        /// </summary>
        public Guid? ProfileTemplateId { get; set; }
        public CardTemplate? ProfileTemplate { get; set; }

        public ICollection<ProfileLink> CustomLinks { get; set; } = [];


    }
}
