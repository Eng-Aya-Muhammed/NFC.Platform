using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    /// <summary>
    /// Represents the Root Tenant in the Multi-Tenant architecture.
    /// A Tenant can represent either an Individual or a Company account.
    /// </summary>
    public class Tenant : BaseEntity
    {
        /// <summary>
        /// Gets or sets the name of the tenant.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the tenant is active.
        /// Inactive tenants cannot access their resources.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the associated Company entity if this is a Company Tenant.
        /// For individual accounts, this is null.
        /// </summary>
        public Company? Company { get; set; }

        /// <summary>
        /// Gets or sets the collection of users belonging to this tenant.
        /// </summary>
        public ICollection<User> Users { get; set; } = [];

        // Future extensibility hook:
        // public TenantSettings? Settings { get; set; }
    }
}
