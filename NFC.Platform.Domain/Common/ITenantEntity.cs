using System;

namespace NFC.Platform.Domain.Common
{
    /// <summary>
    /// Contract indicating that the implementing entity is isolated by a TenantId.
    /// </summary>
    public interface ITenantEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the tenant owning this entity.
        /// </summary>
        Guid TenantId { get; set; }
    }
}
