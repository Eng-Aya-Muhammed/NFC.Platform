using System;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Domain.Entities
{
    public class TemplateRequest : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public Guid RequestedByUserId { get; set; }
        public User RequestedByUser { get; set; } = null!;

        public string TemplateName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? ReferenceImageUrl { get; set; }
        public string? Notes { get; set; }
        public TemplateRequestStatus Status { get; set; } = TemplateRequestStatus.Pending;

        /// <summary>
        /// Filled when the design team delivers: the template produced for this request.
        /// </summary>
        public Guid? ProducedTemplateId { get; set; }
        public CardTemplate? ProducedTemplate { get; set; }

        /// <summary>
        /// The card order created automatically when this request is submitted,
        /// so the user can track it from My Orders from day one.
        /// </summary>
        public Guid? LinkedOrderId { get; set; }
        public CardOrder? LinkedOrder { get; set; }
    }
}
