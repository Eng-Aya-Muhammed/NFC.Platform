using System;

namespace NFC.Platform.Application.DTOs.Company;

    public class CompanyProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string CommercialRegistry { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string AdminUserEmail { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int SubscriptionRemainingDays { get; set; }

        // ── Digital profile branding ──────────────────────────────────────────
        public Guid? ProfileTemplateId { get; set; }
    }
