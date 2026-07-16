using System;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Domain.Entities
{
    public class EmployeeImportJob : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public EmployeeImportJobStatus Status { get; set; } = EmployeeImportJobStatus.Pending;

        public string FileName { get; set; } = string.Empty;
        public string ExcelFileUrl { get; set; } = string.Empty;
        public string? ExcelFilePublicId { get; set; }

        // Card Order parameters
        public CardType CardType { get; set; }
        public CardDesignType CardDesignType { get; set; }
        public Guid? PrintTemplateId { get; set; }
        public string? Notes { get; set; }

        // Job metrics & outcomes
        public int TotalRows { get; set; }
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public string? ErrorsJson { get; set; }

        public Guid? CardOrderId { get; set; }
        public CardOrder? CardOrder { get; set; }
    }
}
