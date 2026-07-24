using System;
using System.Collections.Generic;
using NFC.Platform.Domain.Common;

namespace NFC.Platform.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public Guid? TenantId { get; set; }

        public bool IsSystemRole { get; set; } = false;

        public ICollection<RolePermission> RolePermissions { get; set; } = [];
    }
}
