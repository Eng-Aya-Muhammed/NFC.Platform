using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class Company : BaseEntity, ITenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string CommercialRegistry { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid AdminUserId { get; set; }
        public User AdminUser { get; set; } = null!;

        // ── Digital profile branding ─────────────────────────────────────────

        /// <summary>
        /// FK to the CardTemplate that defines the digital profile layout for all company employees.
        /// Set automatically when a custom TemplateRequest is completed, or manually via PATCH /api/company/branding.
        /// </summary>
        public Guid? ProfileTemplateId { get; set; }
        public CardTemplate? ProfileTemplate { get; set; }

        public ICollection<Employee> Employees { get; set; } = [];
    }
}
